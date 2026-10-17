namespace Catalog.Infrastructure.Consul
{
    /// <summary>
    /// Supplies the port the web server actually bound to, so
    /// <see cref="ServiceRegistration"/> can fall back to it when configuration does not pin one.
    /// </summary>
    /// <remarks>
    /// This interface exists purely so the answer to "which port did Kestrel pick?" can be provided
    /// without <c>Catalog.Infrastructure</c> taking a dependency on ASP.NET Core. Reading the real
    /// port means <c>IServer.Features.Get&lt;IServerAddressesFeature&gt;()</c>, and that type lives
    /// in the web stack: referencing it here would drag <c>Microsoft.AspNetCore.App</c> into
    /// <c>Catalog.WorkerService</c>, a console host that must never require a web runtime. So the
    /// question is declared here as an abstraction and answered by the web host, which is the same
    /// dependency rule the rest of the solution follows: Infrastructure defines the contract, the
    /// host that owns the answer implements it.
    /// </remarks>
    public interface IServerPortResolver
    {
        /// <summary>
        /// The port the server bound to, or <see langword="null"/> when it cannot be determined
        /// (no server, dynamic port 0, or a non-URL address such as a named pipe).
        /// </summary>
        int? ResolveBoundPort();
    }
}