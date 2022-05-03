# Reflection with NativeAOT

This sample was mainly created to help explain how to fix [reflection issues with NativeAOT](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/reflection-in-aot-mode.md).

After reading the above link you should hopefully understand why reflection can cause problem with NativeAOT.

One thing that often requires a lot of reflection is JSON serialization. Luckily we already have a way to generate reflection-free JSON serializers with Lambda using [System.Text.Json source generators](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/). See the section titled 'Using source generator for JSON serialization' of [this AWS blog](https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/#Using%20source%20generator%20for%20JSON%20serialization).

To deploy this code, just run `dotnet lambda deploy-function --function-name reflectionTestNativeAOT`

And then test the deployed function with this command (updated for your function ARN, you can use `aws lambda list-functions` to see all your function ARNs) `dotnet lambda invoke-function arn:aws:lambda:us-east-1:1234567890:function:reflectionTestNativeAOT --payload "Hello World"`

You can see that we implement a custom JsonSerializerContext which allows us to deserialize our object without hitting a `MissingMetadataException` at runtime.
