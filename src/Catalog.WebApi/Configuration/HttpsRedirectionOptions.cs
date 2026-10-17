namespace Catalog.WebApi.Configuration
{
    /// <summary>
    /// Controls whether <c>UseHttpsRedirection</c> is added to the pipeline.
    /// </summary>
    /// <remarks>
    /// The middleware was unconditional, which is a bug behind any TLS-terminating proxy and a 307
    /// generator inside a plain-HTTP container. It redirects the request to the HTTPS port it
    /// discovers, and when it cannot discover one (no <c>https_port</c>, no dev certificate, nothing
    /// in <c>ASPNETCORE_URLS</c> mentioning https) it logs a warning and lets the request through —
    /// so the same binary silently redirected in one deployment and not in another, depending on
    /// configuration nobody was tracking.
    /// <para>
    /// Off by default because that is the case that is correct for a container: the ingress (or the
    /// service mesh sidecar) terminates TLS and re-originates plain HTTP to this process, so
    /// redirecting here would bounce a request the proxy just legitimately forwarded. Turn it on
    /// where this process really is the TLS edge — a Kestrel published with a certificate, typically
    /// local development.
    /// </para>
    /// </remarks>
    public class HttpsRedirectionOptions
    {
        public const string SectionName = "HttpsRedirection";

        /// <summary>See the type remarks: the default deployment terminates TLS upstream.</summary>
        public const bool EnabledDefault = false;

        public bool Enabled { get; set; } = EnabledDefault;
    }
}
