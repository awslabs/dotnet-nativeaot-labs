// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace LambdaToNativeAotConverter
{
    public static class Constants
    {
        public const int DotnetTimeoutMilliseconds = 60000;
        public const string NewEntryPointFileName = "EntryPoint.cs";
        public const string DefaultLambdaToolsConfigFileName = "aws-lambda-tools-defaults.json";
        public const string BackupLambdaToolsConfigFileName = "aws-lambda-tools-defaults-backup.json";

        public const string LambdaToolsDefaultContent = 
@"{
  ""Information"": [
    ""This file provides default values for the deployment wizard inside Visual Studio and the AWS Lambda commands added to the .NET Core CLI."",
    ""To learn more about the Lambda commands with the .NET Core CLI execute the following command at the command line in the project root directory."",
    ""dotnet lambda help"",
    ""All the command line options for the Lambda command can be specified in this file."",
    ""For NativeAot Deployments, make sure you're building/deploying from an Amazon Linux 2 Operating System.""
  ],
  ""profile"": """",
  ""region"": """",
  ""configuration"": ""Release"",
  ""function-runtime"": ""provided.al2"",
  ""function-memory-size"": 256,
  ""function-timeout"": 30,
  ""function-handler"": ""bootstrap"",
  ""msbuild-parameters"": ""--self-contained true""
}";
        public const string EntryPointContent =
@"using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateThisToYourOwnNamespace
{{
    public class EntryPoint
    {{
        /// <summary>
        /// The main entry point for the custom runtime.
        /// </summary>
        private static async Task Main()
        {{
            // If this line has build errors, you may need to instantiate your handler's class with the appropriate constructor arguments
            var lambdaBootstrap = LambdaBootstrapBuilder.Create(({0}){1}, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
                .Build();

            await lambdaBootstrap.RunAsync();
        }}
    }}
    
    // We've already added an attribute for the parameter type and return type of your handler (they may not actually be needed such as in the case when your return type is 'string')
    // You will probably need to include the correct using statements at the top of this file to fix the build if the types are not built-in dotnet types
    // You can also add other types that you need to use with JSON serialization. 
    // See 'Using source generator for JSON serialization' at this link for more information https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/
{2} 
    public partial class MyCustomJsonSerializerContext : JsonSerializerContext
    {{
        // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
        // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for
        // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
        // See the Reflection Sample in dotnet-nativeaot-labs for an example of how to fix a MissingMetadataException
    }}
}}
";
    }
};
