namespace Catalog.Infrastructure.Persistence
{
    /// <summary>
    /// The PostgreSQL schemas the persistence model is spread over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The write side (<see cref="DomainModel.DomainDbContext"/>) and the read side
    /// (<see cref="StoredModel.StoredDbContext"/>) share one database and, until this type
    /// existed, one schema as well: both mapped <c>Food</c>, <c>Recipe</c>, <c>MealPlan</c>,
    /// <c>PlanDay</c> and <c>MealTime</c> into <c>public</c>. Two migration histories then tried to
    /// CREATE the same relations and the deploy step died with <c>42P07: relation "Food" already
    /// exists</c>. EF Core InMemory could not catch it because each context gets its own store
    /// there, so the whole unit and integration suite stayed green while a real boot crashed.
    /// </para>
    /// <para>
    /// <b>Why separate schemas and not separate databases.</b> The projection handlers read the
    /// write model and write the read model in the same unit of work, and the seed step exercises
    /// both. Two databases would mean two connection strings, two credentials and cross-database
    /// joins for anyone writing a report. Two schemas keep one database, one credential and one
    /// transaction, while making the CQRS split physical instead of nominal.
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