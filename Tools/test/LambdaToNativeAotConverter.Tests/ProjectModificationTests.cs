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
        public void AddPackage_AddsIfNotExists(string csprojSampleFilePartialPath)
        {
            // Arrange
            string csprojPath = CreateEmptyCsProj(csprojSampleFilePartialPath);
            const string package = "Microsoft.DotNet.ILCompiler --prerelease";

            // Act
            ProjectModificationHelpers.AddPackage(csprojPath, package);

            // Assert
            Assert.Single(Regex.Matches(File.ReadAllText(csprojPath), "<PackageReference Include=\"Microsoft.DotNet.ILCompiler\"")); // Check for exactly 1 instance of the package reference
        }

        [Fact]
        public void AddEntryPoint_AddsWithCorrectHandler()
        {
            // Arrange
            string handlerName = "LambdaToConvert.Function.MySampleLambdaHandler";
            string handlerPath = CreateLambdaHandlerFile();

            // Act
            ProjectModificationHelpers.AddEntryPoint(tempFilePath + "/sample.csproj", handlerPath, handlerName);

            // Assert
            var expectedPath = $"{tempFilePath}/EntryPoint.cs";
            Assert.True(File.Exists(expectedPath));
            var newFileContent = File.ReadAllText(expectedPath);
            Assert.Equal(ExpectedEntryPointContent, newFileContent);

            // Make sure it will compile
            var tempCsProjPathForTestingBuild = $"{tempFilePath}/MyProject.csproj";
            File.WriteAllText(tempCsProjPathForTestingBuild, File.ReadAllText("SampleLambdaExeCsProj.txt"));
            var addIlProcess = Process.Start("dotnet", $"build \"{tempCsProjPathForTestingBuild}\"");
            addIlProcess.WaitForExit(30000);
            Assert.Equal(0, addIlProcess.ExitCode);
        }

        private string CreateLambdaHandlerFile()
        {
            var newPath = $"{tempFilePath}/FunctionHandler{new Random().Next(100000)}.cs";
            File.Copy("SampleHandlerCsFile.txt", newPath);
            return newPath;
        }

        private string CreateEmptyCsProj(string csprojSampleFilePartialPath)
        {
            var newPath = $"{tempFilePath}/{csprojSampleFilePartialPath}{new Random().Next(100000)}.csproj";
            File.Copy(csprojSampleFilePartialPath, newPath);
            return newPath;
        }

        private const string ExpectedEntryPointContent = @"
using Amazon.Lambda.Core;
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
            // If this line has build errors, you may need to instantiate your handler's class with the appropriate constructor arguments, or if your handler is static, reference it statically instead of newing it up
            var lambdaBootstrap = LambdaBootstrapBuilder.Create((Func<string,ILambdaContext, string>) new LambdaToConvert.Function().MySampleLambdaHandler, new DefaultLambdaJsonSerializer())
                .Build();

            await lambdaBootstrap.RunAsync();
        }
    }

    [JsonSerializable(typeof(DateTimeOffset?))] // This is just an example, replace this (and add more attributes) with types that you need to use with JSON serialization 
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