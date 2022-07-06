// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reflection;

public class Function
{
    /// <summary>
    /// The main entry point for the custom runtime.
    /// </summary>
    private static async Task Main()
    {
        Func<string, ILambdaContext, string> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>())
            .Build()
            .RunAsync();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "We use MyCustomJsonSerializerContext to make sure this type can be deserialized.")]
    public static string FunctionHandler(string input, ILambdaContext context)
    {
        var serializedObjectString = "{\"Name\":\"MyName\",\"CreatedDate\":\"2022-04-15T21:25:04.637464+00:00\"}";

        SampleItemWithProperties? deserializedObject = JsonSerializer.Deserialize<SampleItemWithProperties>(serializedObjectString);
        return "Your input: " + input.ToUpper() + $"; deserializedObject Name: {deserializedObject?.Name}" + $"; deserializedObject CreatedDate: {deserializedObject?.CreatedDate}";
    }
}

public class SampleItemWithProperties
{
    public string? Name {get; set;}
    public DateTimeOffset? CreatedDate {get; set;}
}

// To see the runtime error for yourself:
// Comment out this partial class and attributes
// Then replace `new SourceGeneratorLambdaJsonSerializer<MyCustomJsonSerializerContext>()` above with:
// new DefaultLambdaJsonSerializer()
// (the error you can expect to see at runtime is at the bottom of this file, MissingMetadataException)
[JsonSerializable(typeof(SampleItemWithProperties))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(string))]
public partial class MyCustomJsonSerializerContext : JsonSerializerContext
{
    // By using this partial class derived from JsonSerializerContext, we can generate reflection free JSON Serializer code at compile time
    // which can deserialize our class and properties. However, we must attribute this class to tell it what types to generate serialization code for
    // See https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
}

/*
System.Reflection.MissingMetadataException: 'System.Text.Json.Serialization.Converters.NullableConverter<System.DateTimeOffset>' is missing metadata. For more information, please visit http://go.microsoft.com/fwlink/?LinkID=392859
at System.Reflection.Runtime.General.TypeUnifier.WithVerifiedTypeHandle(RuntimeConstructedGenericTypeInfo, RuntimeTypeInfo[]) + 0x98
at System.Text.Json.Serialization.Converters.NullableConverterFactory.CreateValueConverter(Type, JsonConverter) + 0x14
at System.Text.Json.Serialization.JsonConverterFactory.GetConverterInternal(Type, JsonSerializerOptions) + 0x18
at System.Text.Json.JsonSerializerOptions.GetConverterFromType(Type) + 0x1c6
at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey, Func`2) + 0x8a
at System.Text.Json.JsonSerializerOptions.GetConverterFromMember(Type, Type, MemberInfo) + 0x9d
at System.Text.Json.Serialization.Metadata.JsonTypeInfo.AddProperty(MemberInfo, Type, Type, Boolean, Nullable`1, JsonSerializerOptions) + 0xc6
at System.Text.Json.Serialization.Metadata.JsonTypeInfo.CacheMember(Type, Type, MemberInfo, Boolean, Nullable`1, Boolean&, Dictionary`2&) + 0x7d
at System.Text.Json.Serialization.Metadata.JsonTypeInfo..ctor(Type, JsonConverter, JsonSerializerOptions) + 0x34f
at System.Text.Json.JsonSerializerOptions.<RootReflectionSerializerDependencies>g__CreateJsonTypeInfo|14_0(Type, JsonSerializerOptions) + 0x68
at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey, Func`2) + 0x8a
at System.Text.Json.JsonSerializer.GetTypeInfo(JsonSerializerOptions, Type) + 0x6b
at System.Text.Json.JsonSerializer.Serialize[TValue](TValue, JsonSerializerOptions) + 0x32
at CustomRuntime.Function.FunctionHandler(String, ILambdaContext) + 0x7e
at Amazon.Lambda.RuntimeSupport.HandlerWrapper.<>c__DisplayClass44_0`2.<GetHandlerWrapper>b__0(InvocationRequest) + 0x64
at Amazon.Lambda.RuntimeSupport.LambdaBootstrap.<InvokeOnceAsync>d__17.MoveNext() + 0x16e
*/