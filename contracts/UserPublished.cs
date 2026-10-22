using Joseco.Communication.External.Contracts.Message;

namespace Catalog.Contracts.Integration;

/// <summary>
/// External contract announcing that an account was created in the identity service.
/// </summary>
/// <remarks>
/// This is the catalog's OWN copy of the contract. It is deliberately duplicated rather than
/// shared through a package, because the two services must not depend on each other's assemblies:
/// the identity service publishes, the catalog consumes, and neither knows the other's code.
/// That independence is what lets each be deployed, versioned and scaled on its own.
/// <para>
/// A change to either copy is a CONTRACT change, not a refactor. If identity adds a field to
/// <c>UserPublished</c>, this one has to be updated to match before the catalog can read it —
/// and because the deserializer tolerates unknown members, adding a field on the publishing side
/// alone will not break this consumer. Removing or renaming one will.
/// </para>
/// </remarks>
public record UserPublished : IntegrationMessage
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public UserPublished()
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public UserPublished(string? correlationId, string? source)
        : base(correlationId, source)
    {
        UserId = Guid.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        Scopes = Array.Empty<string>();
    }

    /// <summary>
    /// Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form every contract
    /// on the platform uses. Consumers switch on this, never on the CLR type.
    /// </summary>
    public string EventType => "User.Published";

    public Guid UserId { get; init; }

    public string UserName { get; init; }

    public string Email { get; init; }

    public bool IsAdministrator { get; init; }

    /// <summary>Concrete scopes the account holds. Never the wildcard sentinel.</summary>
    public string[] Scopes { get; init; }
}