# Simple Function With Custom Runtime

This sample contains code that has already been converted to build and run natively. If you want to learn more about how it was created, see the main [README](../../README.md).

This code contains a config file `aws-lambda-tools-defaults.json` which tells the .NET Core Lambda Tools how to deploy it. If you don't have those installed they can be installed with `dotnet tool install -g Amazon.Lambda.Tools`

If you haven't already, configure your local AWS profile with `aws configure`

Then you can deploy the function with `dotnet lambda deploy-function` if needed you can also specify other parameters like so `dotnet lambda deploy-function --function-name MySampleNativeFunction --config-file aws-lambda-tools-defaults.json` or show all options with `dotnet lambda deploy-function --help`

This function can be test by calling `dotnet lambda invoke-function {function-name} -p "Hello World"` e.g. `dotnet lambda invoke-function MySampleNativeFunction -p "Hello World"`

You can also try removing the ILCompiler package reference from the csproj, then deploying the function with a different name. This will allow you to easy test the performance between Native and Managed .NET Lambdas.