public class Program 
{
    public static void Main(string[] args)
    {
        // Get input
        string csprojPath;
        string functionHandler;
        string functionHandlerPath;
        if (args.Length == 3)
        {
            csprojPath = args[0].Trim('"').Trim('\'').Trim(' ');
            functionHandler = args[1].Trim('"').Trim('\'').Trim(' ');
            functionHandlerPath = args[2].Trim('"').Trim('\'').Trim(' ');
        }
        else
        {
            csprojPath = InputOutputHelpers.GetCsProjPath();
            functionHandler = InputOutputHelpers.GetFunctionHandler();
            functionHandlerPath = InputOutputHelpers.GetFunctionHandlerPath();
        }

        // Modify csproj
        ProjectModificationHelpers.SetCsProjProperty(csprojPath, "OutputType", "exe");
        ProjectModificationHelpers.SetCsProjProperty(csprojPath, "AssemblyName", "bootstrap");

        // Add needed packages
        ProjectModificationHelpers.AddPackage(csprojPath, "Microsoft.DotNet.ILCompiler --prerelease");
        ProjectModificationHelpers.AddPackage(csprojPath, "Amazon.Lambda.RuntimeSupport");

        // Add new files that are needed
        ProjectModificationHelpers.AddEntryPoint(csprojPath, functionHandlerPath, functionHandler);
        ProjectModificationHelpers.AddLambdaToolsDefaults(csprojPath);

        InputOutputHelpers.WriteSuccess("Your function is finished converting!");
        InputOutputHelpers.WriteSuccess("Make sure you build and deploy it from Amazon Linux 2 (You can use a VM, Docker, or WSL). You should be able to deploy it with the command 'dotnet lambda deploy-function --function-name MyConvertedNativeFunction --config-file aws-lambda-tools-defaults.json'");
    }
}