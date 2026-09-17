namespace Catalog.Tests.ContractTest
{
    /// <summary>
    /// Central definition of the Pact consumer/provider names and the shared pact directory.
    /// Both the consumer tests (which write the pact) and the provider tests (which read it)
    /// must agree on these values so the generated contract is always located correctly.
    /// </summary>
    public static class PactPaths
    {
        public const string Consumer = "catalog-web-client";
        public const string Provider = "catalog-api";

        /// <summary>Full path to the folder where the generated pact files live (committed to the repo).</summary>
        public static readonly string PactDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ContractTest", "pacts"));

        public static readonly string PactFile = Path.Combine(PactDir, $"{Consumer}-{Provider}.json");
    }
}