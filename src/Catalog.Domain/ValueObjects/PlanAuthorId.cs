namespace Catalog.Domain.ValueObjects;

/// <summary>
/// The identity of the account a meal plan belongs to.
/// </summary>
/// <remarks>
/// A value object rather than a raw <see cref="Guid"/> because the author is a business
/// concept, not a column: "this plan has no author" and "this plan is authored by account X" are
/// different states, and a bare Guid cannot tell them apart — an empty Guid looks exactly like a
/// real one until something compares it to something.
/// <para>
/// It holds only the id. The user name is NOT stored here: it is read from the PlanAuthors
/// projection, which identity owns. Copying it into the aggregate would create a second copy of
/// somebody else's data, and the two would drift the moment a user renames themselves.
/// </para>
/// <para>
/// <b>It is nullable on the aggregate, never on the command.</b> A plan the catalogue seeds has no
/// author, and null is what says so — it needs no synthetic "system" user invented for the
/// occasion. A plan created through the API cannot be authorless: the command requires the caller's
/// id, so the rule lives in the application layer rather than as a column constraint that would
/// also have to permit the seed's own rows.
/// </para>
/// </remarks>
public record PlanAuthorId
{
    public Guid Value { get; }

    private PlanAuthorId(Guid value) => Value = value;

    /// <summary>Creates an author reference, refusing the empty guid.</summary>
    /// <exception cref="ArgumentException">The id is <see cref="Guid.Empty"/>.</exception>
    public static PlanAuthorId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "An author reference must identify an account, and the empty guid identifies none.",
                nameof(value));
        }

        return new PlanAuthorId(value);
    }

    public override string ToString() => Value.ToString();
}