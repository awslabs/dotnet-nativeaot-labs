// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace LambdaToNativeAotConverter.Tests
{
    public class ProjectModificationTests : IDisposable
    {
        private const string tempFilePath = "TempTestFiles";

        // setup
        public ProjectModificationTests()
        {
            ClearTempDirectory();
        }

        // teardown
        public void Dispose()
        {
            ClearTempDirectory();
        }

        private static void ClearTempDirectory()
        {
            if (Directory.Exists(tempFilePath))
            {
                Directory.Delete(tempFilePath, true);
            }
            Directory.CreateDirectory(tempFilePath);
        }

        [Theory]
        [InlineData("SampleBlankCsProj.txt")]
        [InlineData("SampleCsProjWithILCompiler.txt")]
        public void AddPackage_AddsIfNotExists(string templatePath)
        {
            // Arrange
            string csprojPath = CreateCsProjFromTemplate(templatePath);
            const string package = "Microsoft.DotNet.ILCompiler --prerelease";

            // Act
            ProjectModificationHelpers.AddPackage(csprojPath, package);

            // Assert
            Assert.Single(Regex.Matches(File.ReadAllText(csprojPath), "<PackageReference Include=\"Microsoft.DotNet.ILCompiler\"")); // Check for exactly 1 instance of the package reference
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddEntryPoint_AddsWithCorrectHandler(bool isStatic)
        {
            // Arrange
            string handlerName = "LambdaToConvert.Function.MySampleLambdaHandler";
            string handlerPath = CreateLambdaHandlerFile(isStatic);

            // Act
            ProjectModificationHelpers.AddEntryPoint(Path.Combine(tempFilePath, "MyProject.csproj"), handlerPath, handlerName);

            // Assert
            var expectedPath = Path.Combine(tempFilePath, Constants.NewEntryPointFileName);
            Assert.True(File.Exists(expectedPath));
            var newFileContent = File.ReadAllText(expectedPath);
            Assert.Equal(isStatic ? ExpectedEntryPointContentStatic : ExpectedEntryPointContentNonStatic, newFileContent);

            // Make sure it will compile
            var tempCsProjPathForTestingBuild = Path.Combine(tempFilePath, "MyProject.csproj");
            File.WriteAllText(tempCsProjPathForTestingBuild, File.ReadAllText("SampleLambdaExeCsProj.txt"));
            var addIlProcess = Process.Start("dotnet", $"build \"{tempCsProjPathForTestingBuild}\"");
            addIlProcess.WaitForExit(Constants.DotnetTimeoutMilliseconds);
            Assert.Equal(0, addIlProcess.ExitCode);
        }

        [Fact]
        public void AddLambdaToolDefaults_DoesNotOverwriteExisting()
        {
            // Arrange
            var existingContent = "This is the exisiting content";
            var defaultPath = Path.Combine(tempFilePath, Constants.DefaultLambdaToolsConfigFileName);
            var backupPath = Path.Combine(tempFilePath, Constants.BackupLambdaToolsConfigFileName);
            Assert.False(File.Exists(defaultPath));
            Assert.False(File.Exists(backupPath));
            File.WriteAllText(defaultPath, existingContent);

            // Act
            ProjectModificationHelpers.AddLambdaToolsDefaults(Path.Combine(tempFilePath, "MyProject.csproj"));

            // Assert
            Assert.True(File.Exists(backupPath));
            Assert.Equal(existingContent, File.ReadAllText(backupPath));
            Assert.True(File.Exists(defaultPath));
            Assert.Equal(Constants.LambdaToolsDefaultContent, File.ReadAllText(defaultPath));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void SetCsProjProperty_CanAddOrUpdateProperty(bool propertyAlreadyExists, bool valueNeedsUpdate)
        {
            // Arrange
            var testPropertyName = "MyTestProperty";
            var testPropertyValue = "MyTestValue123";
            var afterValue =
$@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <{testPropertyName}>{testPropertyValue}</MyTestProperty>
  </PropertyGroup>
</Project>";
            var csprojPath = CreateCsProjFromTemplate("SampleBlankCsProj.txt");
            if (propertyAlreadyExists)
            {
                var valueToSetTo = valueNeedsUpdate ? testPropertyValue + "OtherValue" : testPropertyValue;
                File.WriteAllText(csprojPath, afterValue.Replace(testPropertyValue, valueToSetTo));
            }

            // Act
            var result = ProjectModificationHelpers.SetCsProjProperty(csprojPath, testPropertyName, testPropertyValue);

            // Assert
            if (!propertyAlreadyExists || (propertyAlreadyExists && valueNeedsUpdate))
            {
                Assert.True(result);
            }
            else
            {
                Assert.False(result);
            }
            Assert.Equal(afterValue, File.ReadAllText(csprojPath));
        }

        private string CreateLambdaHandlerFile(bool isStatic)
        {
            var newPath = Path.Combine(tempFilePath, $"FunctionHandler.cs");
            File.Copy(isStatic ? "SampleHandlerCsFileStatic.txt" : "SampleHandlerCsFileNonStatic.txt", newPath);
            return newPath;
        }

        private string CreateCsProjFromTemplate(string templatePath)
        {
            var newPath = Path.Combine(tempFilePath, "SampleProject.csproj");
            File.Copy(templatePath, newPath);
            return newPath;
        }

        private const string ExpectedEntryPointContentNonStatic =
@"using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateThisToYourOwnNamespace
{
    public class EntryPoint
    {
        /// <summary>
        /// The main entry point for the custom runtime.
        /// </summary>
        private static async Task Main()
        {
            // If this line has build errors, you may need to instantiate your handler's class with the appropriate constructor arguments
            var lambdaBootstrap = LambdaBootstrapBuilder.Create((Func<string,ILambdaContext, string>) new LambdaToConvert.Function().MySampleLambdaHandler, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
                .Build();

            await lambdaBootstrap.RunAsync();
        }
    }
    
    // We've already added an attribute for the parameter type and return type of your handler (they may not actually be needed such as in the case when your return type is 'string')
    // You will probably need to include the correct using statements at the top of this file to fix the build if the types are not built-in dotnet types
    // You can also add other types that you need to use with JSON serialization. 
    // See 'Using source generator for JSON serialization' at this link for more information https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string))] 
    public partial class MyCustomJsonSerializerContext : JsonSerializerContext
    {
        // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
        // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for
        // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
        // See the Reflection Sample in dotnet-nativeaot-labs for an example of how to fix a MissingMetadataException
    }
}
";

        private const string ExpectedEntryPointContentStatic =
@"using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateThisToYourOwnNamespace
{
    public class EntryPoint
    {
        /// <summary>
        /// The main entry point for the custom runtime.
        /// </summary>
        private static async Task Main()
        {
            // If this line has build errors, you may need to instantiate your handler's class with the appropriate constructor arguments
            var lambdaBootstrap = LambdaBootstrapBuilder.Create((Func<string,ILambdaContext, string>)LambdaToConvert.Function.MySampleLambdaHandler, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
                .Build();

            await lambdaBootstrap.RunAsync();
        }
    }
    
    // We've already added an attribute for the parameter type and return type of your handler (they may not actually be needed such as in the case when your return type is 'string')
    // You will probably need to include the correct using statements at the top of this file to fix the build if the types are not built-in dotnet types
    // You can also add other types that you need to use with JSON serialization. 
    // See 'Using source generator for JSON serialization' at this link for more information https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(string))] 
    public partial class MyCustomJsonSerializerContext : JsonSerializerContext
    {
        // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
        // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for
        // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
        // See the Reflection Sample in dotnet-nativeaot-labs for an example of how to fix a MissingMetadataException
    }
}
";
    }
}