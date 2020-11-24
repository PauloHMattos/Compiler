using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EV2.Tests.Snippets
{
    public class ConsoleOutputTests
    {
        private const string SamplesPath = @"../../../../Samples/";

        private readonly ITestOutputHelper _output;

        public ConsoleOutputTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("HelloWorld", null)]
        [InlineData("Input", "Paulo", "24")]
        [InlineData("IsEven", "2", "10", "5")]
        [InlineData("Enum")]
        public async Task SamplesTests(string filenamePrefix, params string[] inputs)
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                ErrorDialog = false,
                WorkingDirectory = Path.Combine(Environment.CurrentDirectory, SamplesPath, filenamePrefix),
                Arguments = "run",
                FileName = "dotnet"
            };

            _output.WriteLine(Environment.CurrentDirectory);
            _output.WriteLine(psi.WorkingDirectory);

            var output = new StringBuilder();
            using Process? process = Process.Start(psi);
            Assert.NotNull(process);

            using ManualResetEvent mreOut = new ManualResetEvent(false), mreErr = new ManualResetEvent(false);

            process!.OutputDataReceived += (o, e) =>
            {
                if (e.Data == null)
                {
                    mreOut.Set();
                }
                else
                {
                    output.Append(e.Data);
                    output.Append(Environment.NewLine);
                }
            };
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (o, e) =>
            {
                if (e.Data == null)
                {
                    mreErr.Set();
                }
                else
                {
                    output.Append(e.Data);
                    output.Append(Environment.NewLine);
                }
            };
            process.BeginErrorReadLine();

            if (inputs != null && inputs.Length > 0)
            {
                foreach (var input in inputs)
                {
                    process.StandardInput.WriteLine(input);
                    //mreOut.WaitOne();
                }
            }

            process.StandardInput.Close();
            process.WaitForExit();

            mreOut.WaitOne();
            mreErr.WaitOne();

            // Compare stdout to outputfile
            var outputPath = Path.GetFullPath(Path.Combine(SamplesPath, filenamePrefix, filenamePrefix + ".out"));
            Assert.Equal(await File.ReadAllTextAsync(outputPath), output.ToString());
        }
    }
}