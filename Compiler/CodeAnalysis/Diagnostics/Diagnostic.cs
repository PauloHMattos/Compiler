using Compiler.CodeAnalysis.Text;

namespace Compiler.CodeAnalysis.Diagnostics
{

    public sealed class Diagnostic
    {
        public TextLocation Location { get; }
        public string Message { get; }

        public Diagnostic(TextLocation location, string message)
        {
            Location = location;
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}