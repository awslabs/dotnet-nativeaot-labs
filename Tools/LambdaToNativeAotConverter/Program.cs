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
        ProjectModificationHelpers.AddEntryPoint(functionHandlerPath, functionHandler);
        ProjectModificationHelpers.AddLambdaToolsDefaults(csprojPath);

        return;
    }
}