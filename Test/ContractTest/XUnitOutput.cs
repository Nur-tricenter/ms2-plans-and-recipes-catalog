using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace Catalog.Tests.ContractTest
{
    /// <summary>
    /// Routes Pact log output to the xUnit test output so contract logs are visible per test.
    /// </summary>
    public class XUnitOutput : IOutput
    {
        private readonly ITestOutputHelper _output;

        public XUnitOutput(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string line) => _output.WriteLine(line);
    }
}