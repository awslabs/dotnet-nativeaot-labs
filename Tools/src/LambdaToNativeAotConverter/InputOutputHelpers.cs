// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace LambdaToNativeAotConverter
{
    internal static class InputOutputHelpers
    {
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void GetConsentToUpgradeInPlace()
        {
            string? agreement = null;
            while (agreement == null)
            {
                Console.WriteLine("This program will upgrade the given project in place, so it is recommended to use source control and/or backup your existing code before running the conversion. Do you agree? (Type \"yes\" to continue)");
                agreement = Console.ReadLine()?.Trim('"').Trim('\'').Trim(' ');
                Console.WriteLine();
            }
            if (!agreement.Equals("yes"))
            {
                WriteError("Please make a backup or commit your project before running this program.");
                Environment.Exit(1);
            }
        }

        public static string GetCsProjPath()
        {
            string? csprojPath = null;
            while (csprojPath == null)
            {
                Console.WriteLine("Enter full path to .csproj file (i.e. 'C:\\Code\\MyRepo\\MyProject\\MyProject.csproj')");
                csprojPath = Console.ReadLine()?.Trim('"').Trim('\'').Trim(' ');
                Console.WriteLine();
            }

            if (!File.Exists(csprojPath))
            {
                WriteError("No file found that coresponds to given csproj path, please check file exists.");
                Environment.Exit(1);
            }

            if (!csprojPath.ToLowerInvariant().EndsWith(".csproj"))
            {
                WriteError("Given csproj path does not end with '.csproj' make sure a valid csproj file was given.");
                Environment.Exit(1);
            }

            return csprojPath;
        }

        public static string GetFunctionHandler()
        {
            string? functionHandler = null;
            while (functionHandler == null)
            {
                Console.WriteLine("Enter function handler method's fully qualified name (with namespace i.e. 'MyNamespace.MyClass.MyHandler'.) If your Lambda project output type is already exe, then just enter any non-empty value here.");
                functionHandler = Console.ReadLine()?.Trim('"').Trim('\'').Trim(' ');
                Console.WriteLine();
            }

            return functionHandler;
        }

        public static string GetFunctionHandlerPath()
        {
            string? functionHandlerPath = null;
            while (functionHandlerPath == null)
            {
                Console.WriteLine("Enter path to cs file that contains your function handler (i.e. 'C:\\Code\\MyRepo\\MyProject\\Handler.cs')  If your Lambda project output type is already exe, then just enter any non-empty value here.");
                functionHandlerPath = Console.ReadLine()?.Trim('"').Trim('\'').Trim(' ');
                Console.WriteLine();
            }

            return functionHandlerPath;
        }
    }
}