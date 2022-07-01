# Reflection with NativeAOT

This sample was mainly created to help explain how to fix [reflection issues with NativeAOT](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/reflection-in-aot-mode.md). After reading that link you should hopefully understand why reflection can cause problem with NativeAOT.

One thing that often requires a lot of reflection is JSON serialization. Luckily we already have a way to generate reflection-free JSON serializers with Lambda using [System.Text.Json source generators](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/). See the section titled 'Using source generator for JSON serialization' of [this AWS blog](https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/#Using%20source%20generator%20for%20JSON%20serialization).

This code should work as-is. To deploy and test this code
* Make sure you have [configured your AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html)
* Make sure you are on AL2, either through docker or WSL (see main [README](../../README.md) for more details)
* Navigate your terminal to the project root folder
* Run `dotnet lambda deploy-function --function-name reflectionTestNativeAOT` to deploy
* Run `aws lambda list-functions` to list your functions, and take the ARN of your new function and put it into the next command
* Run `dotnet lambda invoke-function arn:aws:lambda:us-east-1:1234567890:function:reflectionTestNativeAOT --payload "Hello World"` (Replacing the ARN with your real ARN) to test the function. You can also log into the AWS web console and test there. 

Looking at the code, you can see that we implement a custom JsonSerializerContext which allows us to deserialize our object without hitting a `MissingMetadataException` at runtime.

If you want to see the MissingMetadataException for yourself, just comment out the partial class called `MyCustomJsonSerializerContext` along with its attributes. Then replace `new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>()` with `new DefaultLambdaJsonSerializer()`. This will use reflection to do the deserialization. Then System.Text.Json will attempt to dynamically create a `NullableConverter<DateTimeOffset>` to do the deserialization, but since `NullableConverter<DateTimeOffset>` is not used anywhere in the code and has already been trimmed out, it won't know how to do it.

You can also fix this error without a custom JSON serializer. You might want to do this for other errors in the case where it's not related to serialization or you don't have access to modify the underlying library. To do this, you can use an [rd.xml](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/rd-xml-format.md) file. We've already included the `rd.xml` file in the project folder which you need. All you have to do it uncomment out the `ItemGroup` in the .csproj file that contains `<RdXmlFile Include="rd.xml" />` to actually use it. With this file, we're telling the compiler ahead of time, that we need to know how to construct a `NullableConverter` of type `DateTimeOffset`. Now, with the rd.xml file, you can use the `DefaultLambdaJsonSerializer` and still not hit a `MissingMetadataException` at runtime. This should give you a path to fix all NativeAOT specific runtime errors.