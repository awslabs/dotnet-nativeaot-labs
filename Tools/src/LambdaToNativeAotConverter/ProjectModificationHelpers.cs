using Microsoft.CodeAnalysis.CSharp;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using LambdaToNativeAotConverter;

public static class ProjectModificationHelpers
{

    public static void AddPackage(string csprojPath, string package)
    {
        var addIlProcess = Process.Start("dotnet", $"add \"{csprojPath}\" package {package}");
        if (!addIlProcess.WaitForExit(60000) || addIlProcess.ExitCode != 0)
        {
            InputOutputHelpers.WriteError($"Failed to add package {package} to csproj at {csprojPath}, please add manually with 'dotnet add package {package}'");
        }
    }

    public static void AddEntryPoint(string csprojPath, string handlerFilePath, string handlerFullName)
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
        .OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.Text == handlerFullName.Split('.').Last());

        if (!matchingSyntaxes.Any())
        {
            InputOutputHelpers.WriteError($"Could not find handler name {handlerFullName} inside path {handlerFilePath}");
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

        var fullHandlerNameParts = handlerFullName.Split('.');
        var handlerShortMethodName = fullHandlerNameParts.Last();
        var handlerFullClassName = string.Join('.', fullHandlerNameParts.Reverse().Skip(1).Reverse());

        var handlerInstance = $" new {handlerFullClassName}()." + handlerShortMethodName;

        var newEntryPointPath = Directory.GetParent(csprojPath)?.ToString() ?? "";
        File.WriteAllText(Path.Combine(newEntryPointPath, "EntryPoint.cs"), string.Format(Constants.EntryPointContent, handlerType, handlerInstance));
    }

    public static void AddLambdaToolsDefaults(string path)
    {
        var pathToToolsDefaults = Path.Combine(Directory.GetParent(path)?.ToString() ?? "", "aws-lambda-tools-defaults.json");

        if (File.Exists(pathToToolsDefaults))
        {
            File.Copy(pathToToolsDefaults, pathToToolsDefaults + "-old.json");
            InputOutputHelpers.WriteWarning("It looks like you already had a aws-lambda-tools-defaults.json file. It's been renamed to aws-lambda-tools-defaults.json-old.json, please merge it with the new NativeAOT-compatible file that has replaced it. " +
                "For NativeAOT, you will need to keep these settings as-is function-runtime:provided.al2, function-handler:bootstrap, msbuild-parameters:--self-contained true");
        }

        File.WriteAllText(pathToToolsDefaults, Constants.LambdaToolsDefaultContent);
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