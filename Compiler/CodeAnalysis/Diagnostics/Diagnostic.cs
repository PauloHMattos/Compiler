using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Diagnostics
{
    public readonly struct Diagnostic
    {
        public bool IsError { get; }
        public TextLocation Location { get; }
        public string Message { get; }

        private Diagnostic(bool isError, TextLocation location, string message)
        {
            IsError = isError;
            Location = location;
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }

        public static Diagnostic Error(TextLocation location, string message)
        {
            return new Diagnostic(true, location, message);
        }

        public static Diagnostic Warning(TextLocation location, string message)
        {
            return new Diagnostic(false, location, message);
        }
    }
}