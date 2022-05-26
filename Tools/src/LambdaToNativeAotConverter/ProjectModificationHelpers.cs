// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using Microsoft.CodeAnalysis.CSharp;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace LambdaToNativeAotConverter
{
    public static class ProjectModificationHelpers
    {
        /// <summary>
        /// Calls 'dotnet add package' on the given csproj file for the given package
        /// </summary>
        /// <param name="csprojPath">Path to csproj file</param>
        /// <param name="package">The full name of the package to add</param>
        public static void AddPackage(string csprojPath, string package)
        {
            var addIlProcess = Process.Start("dotnet", $"add \"{csprojPath}\" package {package}");
            if (!addIlProcess.WaitForExit(Constants.DotnetTimeoutMilliseconds) || addIlProcess.ExitCode != 0)
            {
                InputOutputHelpers.WriteError($"Failed to add package {package} to csproj at {csprojPath}, please add manually with 'dotnet add package {package}'");
            }
        }

        /// <summary>
        /// Adds a new file EntryPoint.cs into the same directory as the csproj file. This new file will have a main method entry point that bootstraps a lambda which will be able to handle the given handler
        /// </summary>
        /// <param name="csprojPath">Path to csproj</param>
        /// <param name="handlerFilePath">Path to the .cs file which defines the handler</param>
        /// <param name="handlerFullName">The fully qualified name of the handler (with namespace) that this lambda uses</param>
        public static void AddEntryPoint(string csprojPath, string handlerFilePath, string handlerFullName)
        {
            SyntaxTree? tree = CSharpSyntaxTree.ParseText(File.ReadAllText(handlerFilePath));

            MethodDeclarationSyntax? handlerSyntax = tree.GetCompilationUnitRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.Text == handlerFullName.Split('.').Last()).FirstOrDefault();

            if (handlerSyntax == null)
            {
                InputOutputHelpers.WriteError($"Could not find handler name {handlerFullName} inside path {handlerFilePath}");
                return;
            }
            var returnType = handlerSyntax.ReturnType.ToString();

            List<string?> typesForJsonSerializableAttributes = new();
            List<string> parameterTypes = handlerSyntax.ParameterList.Parameters.Select(x => x.Type.ToString()).ToList();
            if (parameterTypes.Count == 2)
            {
                // The second parameter is always an ILambdaContext, so just take the user's first parameter
                typesForJsonSerializableAttributes.Add(parameterTypes.First());
            }

            string handlerType = "";
            if (returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                handlerType = $"Action<{string.Join(',', parameterTypes)}>";
            }
            else
            {
                handlerType = $"Func<{string.Join(',', parameterTypes)}, {returnType}>";
                if (!returnType.Equals("task", StringComparison.OrdinalIgnoreCase))
                {
                    typesForJsonSerializableAttributes.Add(returnType);
                }
            }

            var isStatic = handlerSyntax.Modifiers.Any(x => x.Text == "static");
            string handlerInstance;
            if (isStatic)
            {
                handlerInstance = handlerFullName;
            }
            else
            {
                var fullHandlerNameParts = handlerFullName.Split('.');
                var handlerShortMethodName = fullHandlerNameParts.Last();
                var handlerFullClassName = string.Join('.', fullHandlerNameParts.Reverse().Skip(1).Reverse());
                handlerInstance = $" new {handlerFullClassName}()." + handlerShortMethodName;
            }

            var newEntryPointPath = Directory.GetParent(csprojPath)?.ToString() ?? "";
            var jsonSerializableAttributes = string.Join(Environment.NewLine, typesForJsonSerializableAttributes.Select(x => $"    [JsonSerializable(typeof({x}))]"));
            File.WriteAllText(Path.Combine(newEntryPointPath, Constants.NewEntryPointFileName), string.Format(Constants.EntryPointContent, handlerType, handlerInstance, jsonSerializableAttributes));
        }

        /// <summary>
        /// Adds a NativeAOT compatible json config file for deploying using the dotnet lambda tools
        /// </summary>
        /// <param name="csprojPath">The path to the csproj. We will put the new config file in the same directory as the csproj file.</param>
        public static void AddLambdaToolsDefaults(string csprojPath)
        {
            var pathToToolsDefaults = Path.Combine(Directory.GetParent(csprojPath)?.ToString() ?? "", Constants.DefaultLambdaToolsConfigFileName);
            var pathToToolsBackup = Path.Combine(Directory.GetParent(csprojPath)?.ToString() ?? "", Constants.BackupLambdaToolsConfigFileName);

            if (File.Exists(pathToToolsDefaults))
            {
                File.Copy(pathToToolsDefaults, pathToToolsBackup);
                InputOutputHelpers.WriteWarning($"It looks like you already had a {Constants.DefaultLambdaToolsConfigFileName} file. It's been renamed to {Constants.BackupLambdaToolsConfigFileName}, " +
                    $"please merge it with the new NativeAOT-compatible file that has replaced it. " +
                    "For NativeAOT, you will need to keep these settings as-is function-runtime:provided.al2, function-handler:bootstrap, msbuild-parameters:--self-contained true");
            }

            File.WriteAllText(pathToToolsDefaults, Constants.LambdaToolsDefaultContent);
        }

        /// <summary>
        /// Updates the propertyName to the propertyValue inside the first PropertyGroup in the csproj file.
        /// </summary>
        /// <param name="csprojPath">Path to the csproj file that will get updated</param>
        /// <param name="propertyName">The name of the property to update or add, example "AssemblyName"</param>
        /// <param name="propertyValue">The value to update the property to, example "bootstrap"</param>
        /// <returns>true if the property value was added or changed, false if no change was needed</returns>
        public static bool SetCsProjProperty(string csprojPath, string propertyName, string propertyValue)
        {
            var wasUpdateNeeded = false;

            XmlDocument xmlDoc = new();
            xmlDoc.Load(csprojPath);

            var existingMatchingPropertyNodes = xmlDoc.GetElementsByTagName(propertyName);
            // This property type doesn't exist yet, add a line for it
            if (existingMatchingPropertyNodes == null || existingMatchingPropertyNodes.Count == 0)
            {
                var properyGroup = xmlDoc.GetElementsByTagName("PropertyGroup")[0];

                if (properyGroup == null)
                {
                    InputOutputHelpers.WriteError("csproj does not have any property groups, please check that this is a valid csproj file.");
                    Environment.Exit(1);
                }

                //Create a new node.
                XmlElement elem = xmlDoc.CreateElement(propertyName);
                elem.InnerText = propertyValue;

                //Add the node to the document.
                properyGroup.AppendChild(elem);
                wasUpdateNeeded = true;
            }
            // This property type already exists, check that it only exists once, and make sure it's the correct value
            else
            {
                if (existingMatchingPropertyNodes.Count > 1)
                {
                    InputOutputHelpers.WriteError($"Sorry, we don't support multiple {propertyName} types in the csproj file.");
                    Environment.Exit(1);
                }
                var existingMatchingPropertyNode = existingMatchingPropertyNodes[0];
                if (existingMatchingPropertyNode == null)
                {
                    InputOutputHelpers.WriteError($"Found 1 {propertyName} element in csproj, but it was null. This is unexpected, please check that this is a valid csproj file.");
                    Environment.Exit(1);
                }

                if (!existingMatchingPropertyNode.InnerText.Equals(propertyValue, StringComparison.OrdinalIgnoreCase))
                {
                    wasUpdateNeeded = true;
                    existingMatchingPropertyNode.InnerText = propertyValue;
                }
            }

            if (wasUpdateNeeded)
            {
                xmlDoc.Save(csprojPath);
            }

            return wasUpdateNeeded;
        }
    }
}