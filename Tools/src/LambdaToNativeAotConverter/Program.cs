namespace LambdaToNativeAotConverter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Get input
            string csprojPath;
            string functionHandler;
            string functionHandlerPath;
            if (args.Length == 4)
            {
                if (!args[0].Trim('"').Trim('\'').Trim(' ').Equals("yes"))
                {
                    InputOutputHelpers.WriteError("Please make a backup or commit your project before running this program and then pass in \"yes\" for the first argument once you have.");
                }
                csprojPath = args[1].Trim('"').Trim('\'').Trim(' ');
                functionHandler = args[2].Trim('"').Trim('\'').Trim(' ');
                functionHandlerPath = args[3].Trim('"').Trim('\'').Trim(' ');
            }
            else
            {
                InputOutputHelpers.GetConsentToUpgradeInPlace();
                csprojPath = InputOutputHelpers.GetCsProjPath();
                functionHandler = InputOutputHelpers.GetFunctionHandler();
                functionHandlerPath = InputOutputHelpers.GetFunctionHandlerPath();
            }

            // Modify csproj
            var updatedToExe = ProjectModificationHelpers.SetCsProjProperty(csprojPath, "OutputType", "exe");
            ProjectModificationHelpers.SetCsProjProperty(csprojPath, "AssemblyName", "bootstrap");

            // Add needed packages
            ProjectModificationHelpers.AddPackage(csprojPath, "Microsoft.DotNet.ILCompiler --prerelease");
            ProjectModificationHelpers.AddPackage(csprojPath, "Amazon.Lambda.RuntimeSupport");

            // Add new files that are needed
            if (updatedToExe)
            {
                ProjectModificationHelpers.AddEntryPoint(csprojPath, functionHandlerPath, functionHandler);
            }
            ProjectModificationHelpers.AddLambdaToolsDefaults(csprojPath);

            InputOutputHelpers.WriteSuccess("Your function is finished converting!");
            InputOutputHelpers.WriteSuccess("Make sure you build and deploy it from Amazon Linux 2 (You can use a VM, Docker, or WSL). You should be able to deploy it with the command 'dotnet lambda deploy-function --function-name MyConvertedNativeFunction --config-file aws-lambda-tools-defaults.json'");
        }
    }
}