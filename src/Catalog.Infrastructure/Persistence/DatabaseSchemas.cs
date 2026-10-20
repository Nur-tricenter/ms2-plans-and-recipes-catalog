namespace Catalog.Infrastructure.Persistence
{
    /// <summary>
    /// The PostgreSQL schemas the persistence model is spread over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The write side (<see cref="DomainModel.DomainDbContext"/>) and the read side
    /// (<see cref="StoredModel.StoredDbContext"/>) share ONE database, separated by schema:
    /// aggregates live in <see cref="Write"/>, projection models in <see cref="Read"/>, and the
    /// transactional outbox in <see cref="Outbox"/>.
    /// </para>
    /// <para>
    /// <b>Why one database and separate schemas.</b> The reference architecture gives each
    /// microservice exactly one database, and this service keeps that: one connection string, one
    /// credential, one migration story. The CQRS split is physical at the schema level
    /// (<c>write</c> vs <c>read</c>) and logical at the code level — every query is served from
    /// <see cref="StoredModel.StoredDbContext"/> and never from the write model — which is what
    /// actually isolates the two models. Two databases would buy a second connection string, a
    /// second credential and cross-database joins for anyone writing a report, in exchange for a
    /// split the code already enforces.
    /// </para>
    /// <para>
    /// <b>Why the names live here.</b> A schema name typed into eight configuration classes is
    /// eight chances to spell it differently, and a mismatch between the write mapping and the
    /// read mapping recreates exactly the bug this type exists to fix. Every <c>ToTable</c> call in
    /// this project takes one of these constants, and <c>PersistenceSchemaSeparationTests</c>
    /// asserts that no two contexts ever map the same table name into the same schema.
    /// </para>
    /// <para>
    /// <b>About <see cref="Outbox"/>.</b> It is not ours to choose.
    /// <c>Joseco.Outbox.EFCore</c> declares <c>OutboxMessageConfig&lt;T&gt;</c> as
    /// <c>internal</c> and calls <c>ToTable("outboxMessage", "outbox")</c> with no extension point,
    /// so the transactional outbox is pinned to that schema on whichever context declares it. The
    /// constant is declared here anyway so the dependency is visible in the same place as the two
    /// schemas we do own, and a test asserts the outbox table never drifts into
    /// <see cref="Write"/>.
    /// </para>
    /// </remarks>
    public static class DatabaseSchemas
    {
        /// <summary>
        /// Default schema of the write side: the aggregates the service writes through.
        /// </summary>
        public const string Write = "write";

        /// <summary>
        /// Default schema of the read side: the projection models the queries are served from.
        /// </summary>
        public const string Read = "read";

        /// <summary>
        /// Schema the transactional outbox lives in. Fixed by <c>Joseco.Outbox.EFCore</c>, not by
        /// this project. See the remarks on this type.
        /// </summary>
        public const string Outbox = "outbox";
    }
}