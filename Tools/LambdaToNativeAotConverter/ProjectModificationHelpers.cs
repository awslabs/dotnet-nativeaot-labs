using Microsoft.CodeAnalysis.CSharp;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

internal class ProjectModificationHelpers
{

    public static void AddPackage(string csprojPath, string package)
    {
        var addIlProcess = Process.Start("dotnet", $"add \"{csprojPath}\" package {package}");
        if (!addIlProcess.WaitForExit(60000) || addIlProcess.ExitCode != 0)
        {
            InputOutputHelpers.WriteError($"Failed to add package {package} to csproj at {csprojPath}, please add manually with 'dotnet add package {package}'");
        }
    }

    public static void AddEntryPoint(string handlerFilePath, string handlerName)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(handlerFilePath));
        var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var compilation = CSharpCompilation.Create("MyCompilation",
            syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
        //Note that we must specify the tree for which we want the model.
        //Each tree has its own semantic model
        var model = compilation.GetSemanticModel(tree);

        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        var matchingSyntaxes = root.DescendantNodes()
        .OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.Text == handlerName.Split('.').Last());

        if (!matchingSyntaxes.Any())
        {
            InputOutputHelpers.WriteError($"Could not find handler name {handlerName} inside path {handlerFilePath}");
        }
        MethodDeclarationSyntax functionHandler = matchingSyntaxes.First();

        var returnType = functionHandler.ReturnType.ToString();
        List<string?> parameterTypes = new();
        foreach (var parameter in functionHandler.ParameterList.Parameters)
        {
            parameterTypes.Add(parameter?.Type?.ToString());
        }
        string handlerType = "";
        if (returnType.ToLowerInvariant().Equals("void"))
        {
            handlerType = $"Action<{string.Join(',', parameterTypes)}>";
        }
        else
        {
            handlerType = $"Func<{string.Join(',', parameterTypes)}, {returnType}>";
        }

        var fullHandlerNameParts = handlerName.Split('.');
        var handlerShortMethodName = fullHandlerNameParts.Last();
        var handlerFullClassName = string.Join('.', fullHandlerNameParts.Reverse().Skip(1).Reverse());

        var handlerInstance = $" new {handlerFullClassName}()." + handlerShortMethodName;

        var fileContent = $@"
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
            var lambdaBootstrap = LambdaBootstrapBuilder.Create(({handlerType}){handlerInstance}, new DefaultLambdaJsonSerializer())
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

        var newEntryPointPath = Directory.GetParent(handlerFilePath)?.ToString() ?? "";
        File.WriteAllText(Path.Combine(newEntryPointPath, "EntryPoint.cs"), fileContent);
    }

    public static void AddLambdaToolsDefaults(string path)
    {
        var fileContent = @"
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
        var pathToToolsDefaults = Path.Combine(Directory.GetParent(path)?.ToString() ?? "", "aws-lambda-tools-defaults.json");

        if (File.Exists(pathToToolsDefaults))
        {
            File.Copy(pathToToolsDefaults, pathToToolsDefaults + "-old.json");
            InputOutputHelpers.WriteWarning("It looks like you already had a aws-lambda-tools-defaults.json file. It's been renamed to aws-lambda-tools-defaults.json-old.json, please merge it with the new NativeAOT-compatible file that has replaced it. " +
                "For NativeAOT, you will need to keep these settings as-is function-runtime:provided.al2, function-handler:bootstrap, msbuild-parameters:--self-contained true");
        }

        File.WriteAllText(pathToToolsDefaults, fileContent);
    }

    public static void SetCsProjProperty(string csprojPath, string propertyName, string propertyValue)
    {
        XmlDocument xmlDoc = new();
        xmlDoc.Load(csprojPath);

        var existingMatchingPropertyNodes = xmlDoc.GetElementsByTagName(propertyName);
        if (existingMatchingPropertyNodes == null || existingMatchingPropertyNodes.Count == 0)
        {
            var properyGroup = xmlDoc.GetElementsByTagName("PropertyGroup")[0];

            if (properyGroup == null)
            {
                Console.WriteLine("csproj should have at least 1 property group");
                return;
            }

            //Create a new node.
            XmlElement elem = xmlDoc.CreateElement(propertyName);
            elem.InnerText = propertyValue;

            //Add the node to the document.
            properyGroup.AppendChild(elem);
        }
        else
        {
            if (existingMatchingPropertyNodes.Count > 1)
            {
                Console.WriteLine($"Sorry, we don't support multiple {propertyName} types in the csproj file yet.");
                return;
            }
            var existingMatchingPropertyNode = existingMatchingPropertyNodes[0];
            if (existingMatchingPropertyNode == null) { throw new ApplicationException($"Found 1 {propertyName} element in csproj, but it was null. This is unexpected."); }

            existingMatchingPropertyNode.InnerText = propertyValue;
        }
        xmlDoc.Save(csprojPath);
    }
}