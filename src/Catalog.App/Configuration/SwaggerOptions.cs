namespace Catalog.App.Configuration
{
    /// <summary>
    /// Controls whether the OpenAPI document and its UI are mapped.
    /// </summary>
    /// <remarks>
    /// This used to be <c>env.IsDevelopment()</c> in <c>Startup.Configure</c>. That reads the wrong
    /// thing: the environment name says how the process was started, not what it should expose. A
    /// deployment that runs <c>ASPNETCORE_ENVIRONMENT=Development</c> because that was the only
    /// working profile published the full API surface, and a developer on a locked-down machine could
    /// not turn it off. An explicit switch is also settable per environment variable
    /// (<c>Swagger__Enabled</c>), which is what the compose stack and the test host use.
    /// </remarks>
    public class SwaggerOptions
    {
        public const string SectionName = "Swagger";

        /// <summary>
        /// Off by default: the OpenAPI document is a description of the entire internal API and has
        /// no business being served in an environment nobody has decided should serve it.
        /// </summary>
        public const bool EnabledDefault = false;

        public bool Enabled { get; set; } = EnabledDefault;
    }
}
