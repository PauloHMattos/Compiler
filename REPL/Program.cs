namespace Compiler.REPL
{
    internal static class Program
    {
        private static void Main()
        {
            var repl = new MyRepl();
            repl.Run();
        }
    }
}