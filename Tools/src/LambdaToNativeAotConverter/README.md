# Lambda to NativeAOT Converter

This tool will convert an existing .NET 6 Lambda project to be a .NET 6 NativeAOT Lambda (see below for .NET 7 changes). It needs to be given the path to your project file, the handler name, and the path to the file where the handler is defined. It does these things:

1. Sets OutputType to exe
1. Sets AssemblyName to bootstrap
1. Adds a package reference to the ILCompiler which will compile the code to linux-native assembly
1. Adds a package reference to the Amazon.Lambda.RuntimeSupport so that we can bootstrap our own executable
1. Adds a main method for the executable to start in
1. Updates or adds a Lambda tool defaults configuration which knows how to deploy as a native executable

This is a console app, to use, just run the project and give the needed input when prompted. **It will upgrade the given project in place, so it is recommended to use source control and/or backup your existing code before running the conversion.**

### .NET 7

If instead you want to use .NET 7, just remove the ILCompiler NuGet package from your project and instead add the `PublishAot` project property [as shown by microsoft](https://docs.microsoft.com/en-us/dotnet/core/deploying/native-aot#publish-native-aot---cli). You can also add `StripSymbols` to automatically strip symbols on Linux into their own file. See the same link for more details.