﻿// --------------------------------------------------------------------------------------------------------------------
// Outcold Solutions (http://outcoldman.com)
// --------------------------------------------------------------------------------------------------------------------

namespace OutcoldSolutions.ConfigTransformationTool
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class Program
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            var argumentsLoader = new ArgumentsLoader();
            if (!argumentsLoader.Load(args))
            {
                return 4;
            }

            OutputLog log = null;
            if (argumentsLoader.Verbose)
                log = OutputLog.FromWriter(Console.Out, Console.Error);
            else if (argumentsLoader.Quiet)
                log = OutputLog.NoLogs();
            else
                log = OutputLog.ErrorOnly(Console.Error);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => log.WriteErrorLine("UnhandledException: {0}.", eventArgs.ExceptionObject);

            try
            {
                if (argumentsLoader.AreAllRequiredParametersSet)
                {
                    var task = new TransformationTask(
                                        log,
                                        argumentsLoader.SourceFilePath,
                                        argumentsLoader.TransformFilePath,
                                        argumentsLoader.PreserveWhitespace);

                    if (argumentsLoader.Indent)
                    {
                        task.Indent = argumentsLoader.Indent;
                        task.IndentChars = argumentsLoader.IndentChars;
                    }

                    if (argumentsLoader.DefaultEncoding != null)
                    {
                        task.DefaultEncoding = argumentsLoader.DefaultEncoding;
                    }

                    if (argumentsLoader.IgnoreMissingTransformation)
                    {
                        task.IgnoreMissingTransformation = true;
                    }

                    IDictionary<string, string> parameters = new Dictionary<string, string>();

                    if (!string.IsNullOrWhiteSpace(argumentsLoader.ParametersString))
                    {
                        var parser = new ParametersParser(log);
                        parser.ReadParameters(argumentsLoader.ParametersString, parameters);
                    }

                    if (!string.IsNullOrWhiteSpace(argumentsLoader.ParametersFile))
                    {
                        ParametersLoader.LoadParameters(argumentsLoader.ParametersFile, parameters);
                    }

                    task.SetParameters(parameters);

                    if (!task.Execute(argumentsLoader.DestinationFilePath, argumentsLoader.ForceParametersTask))
                    {
                        return 4;
                    }
                }
                else
                {
                    ShowToolHelp();
                    return 1;
                }

                return 0;
            }
            catch (Exception e)
            {
                log.WriteErrorLine("Unexpected exception: {0}.", e);
                return 4;
            }
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            var resource = Array.Find(Assembly.GetExecutingAssembly().GetManifestResourceNames(),
                element => element.EndsWith(assemblyName));

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }

        private static void ShowToolHelp()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var exeFile = assembly.ManifestModule.Name;

            Console.WriteLine("{0}, {1}, http://ctt.codeplex.com", GetTitleString(assembly), GetVersionString(assembly));
            Console.WriteLine("by OutcoldSolutions, http://outcoldman.com, {0}", DateTime.Today.Year);
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  source:{file} (s:) - source file path (use stdin to read from console input)");
            Console.WriteLine("  transform:{file} (t:) - transform file path");
            Console.WriteLine("  destination:{file} (d:) - destination file path (use stdout to write to console output)");
            Console.WriteLine(
                "  parameters:{parameters} (p:) - (Optional parameter) \r\n    string of parameters used expected by source file separated by ';',\r\n    value should be separated from name by ':',\r\n    if value contains spaces - quote it");
            Console.WriteLine(
                "  parameters.file:{parameters file} (pf:) - (Optional parameter) \r\n    path to xml file which contains parameters with values \r\n    use xml schema ParametersSchema.xsd to make right file");
            Console.WriteLine(
                "  fpt  - (Optional parameter) force parameters task \r\n    (if parameters argument is empty, but need to apply default values),\r\n    default is false");
            Console.WriteLine(
                "  verbose (v)  - (Optional parameter) verbose output,\r\n    default is false");
            Console.WriteLine(
              "  quiet (q)  - (Optional parameter) no output,\r\n    default is false");
            Console.WriteLine(
                "  preservewhitespace (pw)  - (Optional parameter) preserve whitespace in xml element and xml attribute values,\r\n    default is false");
            Console.WriteLine(
                "  indent (i)  - (Optional parameter) indicating wether the output xml will be indented,\r\n    default is false");
            Console.WriteLine(
                "  indentchars:{chars} (ic)  - (Optional parameter) specify indent chars for xml intending,\r\n    default is 4 whitespaces");
            Console.WriteLine(
                "  encoding:{encoding} (e)  - (Optional parameter) specify default encoding for xml files (utf8, utf16, etc),\r\n    default is utf8");
            Console.WriteLine(
                "  ignoremissingtransform (imt)  - (Optional parameter) ignore missing trasformation file and copy source to destination,\r\n    default is false");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine("{0} source:\"source.config\"", exeFile);
            Console.WriteLine("\ttransform:\"transform.config\"");
            Console.WriteLine("\tdestination:\"destination.config\"");
            Console.WriteLine();
            Console.WriteLine("{0} s:\"source.config\"", exeFile);
            Console.WriteLine("\tt:\"transform.config\"");
            Console.WriteLine("\td:\"destination.config\"");
            Console.WriteLine("\tp:Parameter1:\"Value of parameter1\";Parameter2:Value2");
            Console.WriteLine();
            Console.WriteLine("To get more details about transform file syntax go to");
            Console.WriteLine("http://msdn.microsoft.com/en-us/library/dd465326.aspx");
        }

        private static string GetTitleString(Assembly asm)
        {
            var attributes = asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
            if (attributes.Length > 0 && attributes[0] is AssemblyTitleAttribute)
            {
                return (attributes[0] as AssemblyTitleAttribute).Title;
            }

            return null;
        }

        private static string GetVersionString(Assembly asm)
        {
            if (asm != null && !string.IsNullOrEmpty(asm.FullName))
            {
                var parts = asm.FullName.Split(',');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }
    }
}