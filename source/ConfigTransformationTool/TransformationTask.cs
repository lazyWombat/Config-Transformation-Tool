// --------------------------------------------------------------------------------------------------------------------
// Outcold Solutions (http://outcoldman.com)
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace OutcoldSolutions.ConfigTransformationTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.Web.XmlTransform;

    /// <summary>
    /// Make transformation of file <see cref="SourceFilePath"/> with transform file <see cref="TransformFile"/>.
    /// Look at http://msdn.microsoft.com/en-us/library/dd465326.aspx for syntax of transformation file.
    /// </summary>
    public class TransformationTask
    {
        private readonly OutputLog _log;

        private readonly TransformationLogger _transfomrationLogger;

        private IDictionary<string, string> _parameters;

        private Encoding _defaultEncoding;

        /// <summary>
        /// Empty constructor
        /// </summary>
        private TransformationTask(OutputLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _transfomrationLogger = new TransformationLogger(log);
            IndentChars = "    ";
        }

        /// <summary>
        /// Create new TransformationTask object and set values for <see cref="SourceFilePath"/> and <see cref="TransformFile"/>
        /// </summary>
        /// <param name="log">The logger.</param>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="transformFilePath">Transformation file path</param>
        /// <param name="preserveWhitespace">Force to preserve all whitespaces in Xml Element and Xml Attributes values.</param>
        public TransformationTask(
            OutputLog log, 
            string sourceFilePath, 
            string transformFilePath,
            bool preserveWhitespace)
            : this(log)
        {
            SourceFilePath = sourceFilePath;
            TransformFile = transformFilePath;
            PreserveWhitespace = preserveWhitespace;
        }

        /// <summary>
        /// Source file
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// Transformation file
        /// </summary>
        /// <remarks>
        /// See http://msdn.microsoft.com/en-us/library/dd465326.aspx for syntax of transformation file
        /// </remarks>
        public string TransformFile { get; set; }

        /// <summary>
        /// Force to preserve all whitespaces in Xml Element and Xml Attributes values.
        /// </summary>
        public bool PreserveWhitespace { get; set; }

        /// <summary>
        /// Ignores the missing transform file, skips transformation and generates the source file without changes.
        /// </summary>
        public bool IgnoreMissingTransformation { get; set; }

        /// <summary>
        /// Get or sets a value indicating wether the output Xml will be indented.
        /// </summary>
        public bool Indent { get; set; }

        /// <summary>
        /// Gets or sets the character string to use when indenting. 4 spaces is a default value.
        /// </summary>
        public string IndentChars { get; set; }

        /// <summary>
        /// Gets or sets the default encoding to use.
        /// </summary>
        public Encoding DefaultEncoding
        {
            get => _defaultEncoding ?? Encoding.UTF8;
            set => _defaultEncoding = value;
        }

        /// <summary>
        /// Set parameters and values for transform
        /// </summary>
        /// <param name="parameters">Dictionary of parameters with values.</param>
        public void SetParameters(IDictionary<string, string> parameters)
        {
            _parameters = parameters;
        }

        /// <summary>
        /// Make transformation of file <see cref="SourceFilePath"/> with transform file <see cref="TransformFile"/> to <paramref name="destinationFilePath"/>.
        /// </summary>
        /// <param name="destinationFilePath">File path of destination transformation.</param>
        /// <param name="forceParametersTask">Invoke parameters task even if the parameters are not set with <see cref="SetParameters" />.</param>
        /// <returns>Return true if transformation finish successfully, otherwise false.</returns>
        public bool Execute(string destinationFilePath, bool forceParametersTask = false)
        {
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Destination file can't be empty.", nameof(destinationFilePath));
            }

            _log.WriteLine("Start transformation to '{0}'.", destinationFilePath);

            var isSourceStdIn = "stdin".Equals(SourceFilePath, StringComparison.OrdinalIgnoreCase);
            if (!isSourceStdIn && (string.IsNullOrWhiteSpace(SourceFilePath) || !File.Exists(SourceFilePath)))
            {
                throw new FileNotFoundException("Can't find source file.", SourceFilePath);
            }

            var skipTransformation = false;
            if (string.IsNullOrWhiteSpace(TransformFile) || !File.Exists(TransformFile))
            {
                if (IgnoreMissingTransformation)
                {
                    skipTransformation = true;
                }
                else
                {
                    throw new FileNotFoundException("Can't find transform  file.", TransformFile);
                }
            }

            _log.WriteLine("Source file: '{0}'.", SourceFilePath);
            if (skipTransformation)
            {
                _log.WriteLine("Transform file not found. Copy source to destination.");
            } else
            {
                _log.WriteLine("Transform  file: '{0}'.", TransformFile);
            }

            try
            {
                var encoding = DefaultEncoding;

                bool result;
                string outerXml;

                if (skipTransformation)
                {
                    if (isSourceStdIn)
                    {
                        using (new ConsoleEncodingContext(DefaultEncoding))
                            outerXml = Console.In.ReadToEnd();
                    }
                    else
                    {
                        outerXml = File.ReadAllText(SourceFilePath, DefaultEncoding);
                    }
                    result = true;
                } else
                {
                    var document = new XmlDocument
                    {
                        PreserveWhitespace = PreserveWhitespace
                    };

                    if (isSourceStdIn)
                    {
                        using (new ConsoleEncodingContext(DefaultEncoding))
                            document.Load(Console.In);
                    }
                    else
                    {
                        document.Load(SourceFilePath);
                    }
                    if (document.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
                    {
                        var xmlDeclaration = (XmlDeclaration)document.FirstChild;
                        if (!string.IsNullOrEmpty(xmlDeclaration.Encoding))
                        {
                            encoding = Encoding.GetEncoding(xmlDeclaration.Encoding);
                        }
                    }

                    _log.WriteLine("Transformation task is using encoding '{0}'. Change encoding in source file, or use the 'encoding' parameter if you want to change encoding.", encoding);

                    var transformFile = File.ReadAllText(TransformFile, encoding);

                    if ((_parameters != null && _parameters.Count > 0) || forceParametersTask)
                    {
                        var parametersTask = new ParametersTask();
                        if (_parameters != null)
                        {
                            parametersTask.AddParameters(_parameters);
                        }

                        transformFile = parametersTask.ApplyParameters(transformFile);
                    }

                    var transformation = new XmlTransformation(transformFile, false, _transfomrationLogger);

                    result = transformation.Apply(document);

                    outerXml = document.OuterXml;

                    if (Indent)
                    {
                        outerXml = GetIndentedOuterXml(outerXml, encoding);
                    }

                    if (PreserveWhitespace)
                    {
                        outerXml = outerXml.Replace("&#xD;", "\r").Replace("&#xA;", "\n");
                    }
                }


                var isDestinationStdOut = "stdout".Equals(destinationFilePath, StringComparison.OrdinalIgnoreCase);

                if (isDestinationStdOut)
                {
                    using (new ConsoleEncodingContext(DefaultEncoding))
                        Console.Out.Write(outerXml);
                } else
                {
                    File.WriteAllText(destinationFilePath, outerXml, encoding);
                }

                return result;
            }
            catch (Exception e)
            {
                _log.WriteErrorLine("Exception while transforming: {0}.", e);
                return false;
            }
        }

        private string GetIndentedOuterXml(string xml, Encoding encoding)
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = IndentChars ?? new string(' ', 4),
                Encoding = encoding
            };

            using (var buffer = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(buffer, xmlWriterSettings))
                {
                    XDocument.Parse(xml).WriteTo(xmlWriter);
                }

                return WorkAroundToRestoreProperXmlDeclarationTag(xml, buffer.ToString());
            }
        }

        private string WorkAroundToRestoreProperXmlDeclarationTag(string xml, string indentedXml)
        {
            var xmlRegex = new Regex(@"^(<\?xml.*\?>\s*)", RegexOptions.Singleline);
            var match = xmlRegex.Match(xml);
            return !match.Success
                ? xmlRegex.Replace(indentedXml, string.Empty)
                : xmlRegex.Replace(indentedXml, match.Groups[1].Value);
        }
    }
}