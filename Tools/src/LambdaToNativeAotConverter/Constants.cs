﻿namespace LambdaToNativeAotConverter
{
    public static class Constants
    {
        public const string NewEntryPointFileName = "EntryPoint.cs";
        public const string DefaultLambdaToolsConfigFileName = "aws-lambda-tools-defaults.json";
        public const string BackupLambdaToolsConfigFileName = "aws-lambda-tools-defaults-backup.json";

        public const string LambdaToolsDefaultContent = @"
{
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
}
";
        public const string EntryPointContent = @"
using Amazon.Lambda.Core;
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
            // If this line has build errors, you may need to instantiate your handler's class with the appropriate constructor arguments, or if your handler is static, reference it statically instead of newing it up
            var lambdaBootstrap = LambdaBootstrapBuilder.Create(({0}){1}, new DefaultLambdaJsonSerializer())
                .Build();

            await lambdaBootstrap.RunAsync();
        }}
    }}

    [JsonSerializable(typeof(DateTimeOffset?))] // This is just an example, replace this (and add more attributes) with types that you need to use with JSON serialization 
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