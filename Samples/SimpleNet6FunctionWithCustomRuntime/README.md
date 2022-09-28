# Simple .NET 6 Function With Custom Runtime

This sample contains code that has already been converted to build and run natively. You will need to build on Amazon Linux 2 to deploy it to AWS Lambda. If you want to learn more about how it was created or how to run on Amazon Linux 2, see the main [README](../../README.md).

This code contains a config file `aws-lambda-tools-defaults.json` which tells the .NET Core Lambda Tools how to deploy it. Importantly, in the `aws-lambda-tools-defaults.json` file, we specify that we want to run on a custom AL2 runtime and we also pass extra MSBuild parameters. If you don't have the .NET Core Lambda Tools installed they can be installed with `dotnet tool install -g Amazon.Lambda.Tools`

If you haven't already, configure your local AWS profile with `aws configure`

Also, make sure you have the needed Linux Build Dependencies listed in [Prerequisites](../../README.md#prerequisites) `sudo yum -y install clang krb5-devel openssl-devel zip llvm`

Then you can deploy the function with `dotnet lambda deploy-function` if needed you can also specify other parameters like so `dotnet lambda deploy-function --function-name MySampleNativeFunction --config-file aws-lambda-tools-defaults.json` or show all options with `dotnet lambda deploy-function --help`

For now, using the dotnet lambda tools to deploy, will work, but it doesn't strip the binary and will zip up extra files besides the bootstrap file, so package size will be bigger as compared to stripping and zipping manually.'

This function can be test by calling `dotnet lambda invoke-function {function-name} -p "Hello World"` e.g. `dotnet lambda invoke-function MySampleNativeFunction -p "Hello World"`

You can also try removing the ILCompiler package reference from the csproj, then deploying the function with a different name. This will allow you to easy test the performance between Native and Managed .NET Lambdas. Since this is .NET 6, you need to include the ILCompiler package reference, and cannot use the newer `PublishAot` property.
