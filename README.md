# AWS .NET NativeAOT Labs

## What is the goal of this repository?

We want to use this repository to gather community feedback and questions about using .NET NativeAOT on AWS, especially focused on Lambda. We also want to make this a place where developers can learn how to build with .NET NativeAOT to run their own experiments with it on AWS.

## What is .NET NativeAOT?

At a high level, NativeAOT for .NET is a way to compile your .NET projects directly to machine code, eliminating the Intermediary Language and Just-In-Time compilation. AOT stands for "Ahead of Time", as opposed to "Just in Time". While compiling this way gives less flexibility, it has the ability to improve performance, especially at startup.

While currently still experimental, .NET NativeAOT is expected to be moved into the main .NET runtime as part of [.NET 7](https://github.com/dotnet/runtime/issues/61231).

.NET NativeAOT is based off of the previous [CoreRT repository](https://github.com/dotnet/corert).

## Why use .NET NativeAOT?

### Faster Cold Starts

In our experience, the biggest benefit of compiling directly to a native binary is speeding up the cold start time of an application. We've seen average reduction in cold start times from 20% up to 70% depending on the code. When a IL compiled application runs in the CLR, it needs time to compile Just-In-Time. When run natively there is no JITing. In a serverless function like Lambda, cold start times become much more important since the function can often be spinning up and down.

## How can I try it?

### Prerequisites

1. [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). NativeAOT is only supported on .NET 6 and greater.
1. [Amazon Linux 2](https://aws.amazon.com/amazon-linux-2/?amazon-linux-whats-new.sort-by=item.additionalFields.postDateTime&amazon-linux-whats-new.sort-order=desc). If building for Lambda, you'll need to create the binaries on Linux since cross-OS compilation is not supported with NativeAOT. Using an EC2 instance running Amazon Linux 2 is recommended for building .NET NativeAOT Lambda functions, although we hope to make compiling in containers easier in the future. Other distributions of Linux *may* work, but will probably have compatibility issues with Lambda since Lambda runs on Amazon Linux.
1. [AWS CLI](https://aws.amazon.com/cli/) For easy management of AWS resources from the command line. Make sure to [initialize your credentials](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html). 
1. [Dotnet Amazon Lambda Templates](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html) For creating dotnet lambda projects from the command line. (installed with `dotnet new -i Amazon.Lambda.Templates`)
1. [AWS Visual Studio Toolkit](https://aws.amazon.com/visualstudio/) If using Visual Studio.
1. It may also be helpful to read these documents from the .NET repositories and build a local console app before moving on to Lambda: [Using Native AOT](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/README.md)

### Follow our samples

Once you have the prerequisites, follow one of the examples below to deploy your own NativeAOT Lambda.

1. [Lambda Hello World -ToUpper](Samples/ToUpperFunctionWithCustomRuntime/CustomRuntimeNativeInstructions.md)

## Have issues or suggestions?

If you have any problems or suggestions, just open a GitHub issue right in this repository. See [Reporting Bugs/Feature Requests](CONTRIBUTING.md#reporting-bugsfeature-requests) for instructions. Your feedback could help steer us in the right direction.

## Want to contribute?

If you think you have something to contribute see [CONTRIBUTING](CONTRIBUTING.md)

## Security

See [Security issue notifications](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the [LICENSE](LICENSE) file.
