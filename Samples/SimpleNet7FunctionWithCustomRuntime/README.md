# Simple .NET 7 Function With Custom Runtime

This sample contains code that has already been converted to build and run natively. You will need to run on Amazon Linux 2 to deploy it. If you want to learn more about how it was created or how to run on Amazon Linux 2, see the main [README](../../README.md).

This code contains a config file `aws-lambda-tools-defaults.json` which tells the .NET Core Lambda Tools how to deploy it. Importantly, in the `aws-lambda-tools-defaults.json` file, we specify that we want to run on a custom AL2 runtime and we also pass extra MSBuild parameters. If you don't have the .NET Core Lambda Tools installed they can be installed with `dotnet tool install -g Amazon.Lambda.Tools`

If you haven't already, configure your local AWS profile with `aws configure`

Then you can deploy the function with `dotnet lambda deploy-function` if needed you can also specify other parameters like so `dotnet lambda deploy-function --function-name MySampleNativeFunction --config-file aws-lambda-tools-defaults.json` or show all options with `dotnet lambda deploy-function --help`. If you have any issues using `dotnet lambda` with .NET 7, you can use .NET to manually publish the executable with `dotnet publish -r linux-x64 -c Release --self-contained` and then manually zip the bootstrap file. From there, you can use the `dotnet lambda` tools for .NET 6 on Windows to deploy the package with `dotnet lambda deploy-function --package .\bin\Release\net7.0\linux-x64\native\bootstrap.zip`

For now, using the dotnet lambda tools to deploy, will work, but it will zip up extra files besides the bootstrap file, so package size will be bigger as compared and zipping manually. Since we included `StripSymbols`, the bootstrap binary will be smaller and symbols will be in their own .dbg file.

This function can be test by calling `dotnet lambda invoke-function {function-name} -p "Hello World"` e.g. `dotnet lambda invoke-function MySampleNativeFunction -p "Hello World"`

You can also try removing the `PublishAot` and `StripSymbols` properties from the csproj, then deploying the function with a different name. This will allow you to easy test the performance between Native and Managed .NET Lambdas.
