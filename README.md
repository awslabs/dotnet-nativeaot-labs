# AWS .NET NativeAOT Labs

## What is the goal of this repository?

We want to use this repository to gather community feedback and questions about using .NET NativeAOT on AWS, especially focused on Lambda. We also want to make this a place where developers can learn how to build with .NET NativeAOT to run their own experiments with it on AWS.

## What is .NET NativeAOT?

At a high level, NativeAOT for .NET is a way to compile your .NET projects directly to machine code, eliminating the Intermediary Language and Just-In-Time compilation. AOT stands for "Ahead of Time", as opposed to "Just in Time". While compiling this way gives less flexibility, it has the ability to improve performance, especially at startup.

While currently still experimental, NativeAOT is expected to be moved into the main .NET runtime as part of [.NET 7](https://github.com/dotnet/runtime/issues/61231).

.NET NativeAOT is based off of the previous [CoreRT repository](https://github.com/dotnet/corert).

## Why use .NET NativeAOT?

### Faster Cold Starts

In our experience, the biggest benefit of compiling directly to a native binary is speeding up the cold start time of an application. We've seen average reduction in cold start times from 20% up to 70% depending on the code. When a IL compiled application runs in the CLR, it needs time to compile Just-In-Time. When run natively there is no JITing. In a serverless function like Lambda, cold start times become much more important since the function can often be spinning up and down.

## How can I try it?

Check out some of our pre-built samples or continue below for steps to create a native Lambda from scratch.

1. [Simple Function With Custom Runtime](Samples/SimpleFunctionWithCustomRuntime/README.md)
1. [Build and Run with Containers](Samples/BuildAndRunWithContainers/README.md)
1. [ASP.NET Core Web API](Samples/ASP.NETCoreWebAPI/README.md)

# Building .NET NativeAOT Lambda From Scratch

These instructions will walk you through how to generate, deploy, and test a simple .NET native Lambda Function. The end result should be similar to the [Simple Function With Custom Runtime](Samples/SimpleFunctionWithCustomRuntime/README.md) sample.

## Prerequisites

1. [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). NativeAOT is only supported on .NET 6 and greater.
1. [AWS CLI](https://aws.amazon.com/cli/) For easy management of AWS resources from the command line. Make sure to [initialize your credentials](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html).
1. [Dotnet Amazon Lambda Templates](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html) For creating dotnet lambda projects from the command line. (installed with `dotnet new -i Amazon.Lambda.Templates`)
1. [.NET Global Lambda Tools for AWS](https://aws.amazon.com/blogs/developer/net-core-global-tools-for-aws/) For deploying and invoking your lambda. (installed with `dotnet tool install -g Amazon.Lambda.Tools`)
1. [Amazon Linux 2](https://aws.amazon.com/amazon-linux-2/?amazon-linux-whats-new.sort-by=item.additionalFields.postDateTime&amazon-linux-whats-new.sort-order=desc). If building for Lambda, you'll need to create the binaries on Linux since cross-OS compilation is not supported with NativeAOT. We recommend using AL2 for compatibility with Lambda. You can either use an EC2 instance running AL2 or a Docker container running AL2 (see container sample) or [Amazon Linux 2 for WSL2](https://aws.amazon.com/blogs/developer/developing-on-amazon-linux-2-using-windows/).
1. (Optional) [AWS Visual Studio Toolkit](https://aws.amazon.com/visualstudio/) If using Visual Studio.
1. (Optional) It may also be helpful to read these documents from the .NET repositories and build a local console app before moving on to Lambda: [Using Native AOT](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/README.md)

## Set Up the Sample Function Code

If you haven't already, install the prerequisites from the main [README](../../README.md#prerequisites)

For now, to work with native code, we will need to deploy our Lambda as a custom runtime. This is because there is currently no Lambda-managed .NET NativeAOT runtime. To generate a simple function with a custom runtime, from the command line, run `dotnet new lambda.CustomRuntimeFunction`

At this point, you should be able to deploy the Lambda to AWS and test it, but it will just be compiling as [ReadyToRun](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run) which gives some of the benefits of Ahead of Time compilation, but still uses Just in Time compilation too.

## Understanding the csproj

There are some parts of the csproj worth understanding.

1. PublishReadyToRun is set to true, this should help cold starts somewhat, but not as much as NativeAOT will. We will remove this when converting to NativeAOT.
2. The AssemblyName is set to 'bootstrap' which is the [name of the file Lambda will look for](https://docs.aws.amazon.com/lambda/latest/dg/runtimes-custom.html) by convention. If you don't have this csproj property, you can always manually rename your binary to 'bootstrap' before uploading it.
3. OutputType is 'Exe'. It may be possible in the future to have a separate entry point for native function handlers managed by AWS itself (similar to how common .NET Lambdas work), but for now we will bootstrap ourselves.

## Converting to Native

For a simple function like this, converting to native only takes a few simple steps.

### 1) Remove PublishReadyToRun from csproj

Edit the csproj and remove the line with PublishReadyToRun. Also remove the PublishReadyToRun comment. This isn't needed, and is an alterative compilation mode to NativeAOT.

### 2) Add the experimental NuGet feed

[As documented by Microsoft](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/compiling.md#add-ilcompiler-package-reference). In order to use the currently experimental IL Compiler to compile natively, we need to use the experimental NuGet feed. In the same directory as the csproj, create a file called 'nuget.config' and inside paste the below XML. This will add a new NuGet feed called dotnet-experimental while maintaining your existing NuGet feeds too. If NativeAOT is officially brought into .NET 7, this step will probably change.

```XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <add key="dotnet-experimental" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json" />
 </packageSources>
</configuration>
```

### 3) Referencing the experimental ILCompiler package from the experimental nuget feed

Now we can reference the NuGet package `Microsoft.DotNet.ILCompiler -v 7.0.0-*`. In your csproj add the below PackageReference along with the other already existing PackageReferences.

```XML
    <PackageReference Include="Microsoft.DotNet.ILCompiler" Version="7.0.0-*" />
```

## Compiling as Native

### Test the compilation locally

You should at this point be able to build your project natively on any Operating System (though a binary build on Windows won't run on Lambda). If on Windows, from the command line in the same directory as your csproj, you can run `dotnet publish -r win-x64 -c Release --self-contained`. You can also replace `win` with `linux` or `osx` depending on what OS you are currently on. You can also [build for ARM](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/compiling.md#cross-architecture-compilation) if needed.

This will publish a `Release` build of your code for the specified architecture. [Self contained](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) means the necessary dotnet runtime will be included so the binary can run on a machine without dotnet installed.

You will probably see a lot of AOT analysis warnings during the publish. These are warnings that we hope to reduce in the future. You can check that it worked by looking for a file called `bootstrap` (or `bootstrap.exe` on Windows) in the `bin\Release\net6.0\win-x64\native` folder. It will probably be in the 10's of MBs in size. This is because it includes all the needed parts from the .NET runtime. If you try to run it, it will throw an exception since you're not running it from a Lambda environment.

Optional: Delete the bin and obj directories from your main project folder as we will be copying this folder to a build machine next.

### Compiling for Lambda

To compile a binary suitable to deploy to AWS Lambda, we will need to build on Amazon Linux 2. This will make sure our binary is built for the correct operating system and architecture and that all the correct native libraries are included. For now, we will just spin up an Amazon Linux 2 EC2 instance to do our publish. If you're on Windows, you can instead use [Amazon Linux 2 for WSL2](https://aws.amazon.com/blogs/developer/developing-on-amazon-linux-2-using-windows/). You can also check out the [Contains sample](/Samples/BuildAndRunWithContainers/README.ms) to use Docker containers for publishing and/or running.

1. Create an EC2 instance that uses kernel `5.*` and architecture `64-bit (x86)`. If unsure on instance type, you can use a `t2.xlarge`. **Don't forget to terminate or stop it when done to prevent unnecessary costs.**
1. Download this source code to that instance (git example below, but scp or others methods work too)
    * `sudo yum install git`
    * `git clone https://github.com/owner/repositoryName.git` (you'll need a personal access token if this is a private repo)
1. Install .NET CLI
    * Install the Microsoft package repository
    * `sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm`
    * Install the .NET 6 SDK
    * `sudo yum install dotnet-sdk-6.0`
1. Install build dependencies
    * `sudo yum -y install clang krb5-devel openssl-devel`
1. Navigate into the directory that contains your csproj
1. Do the publish (file will be built to bin/release/net6.0/linux-x64/native/)
    * `dotnet publish -r linux-x64 -c release --self-contained`

## Deploying to Lambda

1. If your binary is too large now that it is packaged with the .NET runtime, you can strip symbol files from it. This isn't need on Windows, since exes have symbol in a separate file, (.pdb). Lambda code size limits are documented [here](https://docs.aws.amazon.com/lambda/latest/dg/gettingstarted-limits.html).
    * `strip bin/Release/net6.0/linux-x64/native/bootstrap`
1. Copy the 'bootstrap' entrypoint file into your working directory (same directory as csproj)
    * `cp bin/Release/net6.0/linux-x64/native/bootstrap bootstrap`
1. Zip up the bootstrap file to send to Lambda
    * `zip package.zip bootstrap`
1. Create (or update) the AWS Lambda function that will run this code
    * If you already deployed the sample non-natively, and want to use that Lambda function for the native code, you'll need to change the Runtime Setting from '.NET 6 (C#/PowerShell)' to 'Custom runtime on Amazon Linux 2'
    * If you want to start with a new Lambda function, create one matching the architecture of your build machine (likely x86_64) with runtime 'Custom runtime on Amazon Linux 2'
1. Upload the zip file to your function.
    * You can manually upload the zip file through the AWS Web Console
    * Or if you have the AWS CLI installed and a profile configured on your build machine, you can run the below command after updating the function-name parameter.
        * `aws lambda update-function-code --function-name arn:aws:lambda:us-east-1:123456789:function:myAwesomeNativeFunction --zip-file fileb://package.zip`

## Testing your function

For this sample, you can easily test the function from the AWS Web Console by navigating to the function, then going into the 'Test' tab, then entering a test string like `"Hello World"` into the 'Event JSON' section, then clicking 'Test'.

In the output, you should see your input string in all upper case letters along with function run durations and other meta data.

You can also run the function from the command line with this command and monitor in CloudWatch instead: `aws lambda invoke --function-name arn:aws:lambda:us-east-1:123456789:function:myAwesomeNativeFunction --payload "\"Hello World\"" response.json && cat response.json`

To force a cold start, update any configuration on the Lambda and wait for it to apply. For example, you can change the timeout value (it needs to be a new timeout value each time to actually change the function): `aws lambda update-function-configuration --function-name arn:aws:lambda:us-east-1:123456789:function:myAwesomeNativeFunction --timeout 60`

### Next Steps

If you want to run your own code with NativeAOT it is likely you will need to fix some runtime reflection errors. See the ASP.NET Core Web API sample for deal with reflection errors.  

## Common Errors

### Couldn't find valid bootstrap(s)

```JSON
{
 "errorMessage": "RequestId: XXXXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXX Error: Couldn't find valid bootstrap(s): [/var/task/bootstrap /opt/bootstrap]",
 "errorType": "Runtime.InvalidEntrypoint"
}
```

There are 2 common causes of this error:

1. The uploaded file wasn’t called ‘bootstrap’ or wasn’t in the root directory of the uploaded zip file. Make sure not to zip up subdirectories.
1. You've accidentally published a binary that wasn’t linux-x86_64 on a x86_64 Lambda or wasn't linux-arm64 on a AMR64 Lambda. Make sure your Lambda and published binary have matching operating systems and architectures.

### Build Hangs or is Slow

One trade off with Ahead of Time compilation is that a lot of work has to be done at compile time. A simple application can easy use over 8 GB of memory while compiling and use the majority of a modern multi-core CPU. Try increasing build machine specs if you think this might be the case.

### Runtime error about missing metadata

If the missing method or class appears to be related to JSON serialization you can use [Source generation for JSON serialization](https://docs.aws.amazon.com/lambda/latest/dg/csharp-handler.html). This will give the JSON serializer the ability to work without any metadata for that type using instead generated code and no reflection.

Otherwise, if the exception is just about some other reflection you can specify the type in the RD.xml file to force metadata generation for it. For more information see the [runtime labs readme](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/rd-xml-format.md) or the [Microsoft docs](https://docs.microsoft.com/en-us/windows/uwp/dotnet-native/runtime-directives-rd-xml-configuration-file-reference)

Check out our ASP.NET Core Web API sample to see examples of dealing with runtime reflection errors.

## Have issues or suggestions?

If you have any problems or suggestions, just open a GitHub issue right in this repository. See [Reporting Bugs/Feature Requests](CONTRIBUTING.md#reporting-bugsfeature-requests) for instructions. Your feedback could help steer us in the right direction.

## Want to contribute?

If you think you have something to contribute see [CONTRIBUTING](CONTRIBUTING.md)

## Security

See [Security issue notifications](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the [LICENSE](LICENSE) file.
