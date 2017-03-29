using System;
using System.Text;

namespace OutcoldSolutions.ConfigTransformationTool
{
    public class ConsoleEncodingContext : IDisposable
    {
        private readonly Encoding _inputEncoding;
        private readonly Encoding _outputEncoding;

        public ConsoleEncodingContext(Encoding encoding)
        {
            _inputEncoding = Console.InputEncoding;
            _outputEncoding = Console.OutputEncoding;

            Console.InputEncoding = encoding;
            Console.OutputEncoding = encoding;
        }
        public void Dispose()
        {
            Console.InputEncoding = _inputEncoding;
            Console.OutputEncoding = _outputEncoding;
        }
    }
}
