namespace Catalog.Infrastructure.Persistence.StoredModel.Entities;

/// <summary>
/// A user the catalog knows about, projected from the identity service's UserPublished event.
/// </summary>
/// <remarks>
/// A projection, not an aggregate: the catalog never creates or modifies this. It exists so a
/// meal plan can name its author and so the API can answer "who can I create a plan for?" without
/// calling the identity service on every request.
/// <para>
/// Identity is the owner of this data. If the two ever disagree, identity is right — this table is
/// a cache of somebody else's truth, rebuilt by replaying the exchange.
/// </para>
/// </remarks>
public class PlanAuthorModel
{
    protected PlanAuthorModel() { }

    public PlanAuthorModel(Guid userId, string userName, string email, bool isAdministrator)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
        IsAdministrator = isAdministrator;
        SeenOn = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the account holds the wildcard scope. Kept because the catalog's own authorization
    /// asks "may this user manage others' plans", and answering that from a per-scope list would
    /// mean comparing against every scope this service knows about.
    /// </summary>
    public bool IsAdministrator { get; private set; }

    public DateTime SeenOn { get; private set; }

    /// <summary>
    /// Applies a later projection of the same account.
    /// </summary>
    /// <remarks>
    /// Kept separate from the constructor so a re-delivered or renamed event refreshes the fields
    /// identity owns without recreating the row. Every field on this type belongs to identity —
    /// there is no catalog-owned state to preserve, which is exactly why this model carries no
    /// counters. A "how many plans does this author have" column would have to be incremented from
    /// the command side, making this table a write target that an event replay could contradict.
    /// </remarks>
    public void Refresh(string userName, string email, bool isAdministrator)
    {
        UserName = userName;
        Email = email;
        IsAdministrator = isAdministrator;
        SeenOn = DateTime.UtcNow;
    }
}