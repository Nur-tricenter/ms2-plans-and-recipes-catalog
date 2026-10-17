using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Catalog.Tests.Infrastructure.Persistence
{
    /// <summary>
    /// Guards the physical separation between the write side (<see cref="DomainDbContext"/>) and
    /// the read side (<see cref="StoredDbContext"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The defect this exists for.</b> Both contexts mapped the same table names into the same
    /// <c>public</c> schema, so both migration histories tried to CREATE the same relations and the
    /// deploy step died against a real PostgreSQL with
    /// <c>42P07: relation "Food" already exists</c>. Nothing in the unit or integration suite
    /// noticed, because EF Core InMemory gives each context its own store — the collision is
    /// invisible to it by construction. A green suite therefore said nothing about this defect,
    /// which is exactly why the assertion has to read the relational model rather than trust a
    /// passing run.
    /// </para>
    /// <para>
    /// <b>Why Npgsql and not InMemory.</b> <see cref="IRelationalModel"/> — the thing that resolves
    /// table names to schema-qualified relations — only exists for a relational provider. The
    /// contexts here are built with the Npgsql provider and a placeholder connection string that is
    /// never opened: model building is entirely offline, so no database is needed and none is
    /// touched.
    /// </para>
    /// </remarks>
    public class PersistenceSchemaSeparationTests
    {
        private const string DesignTimeConnectionString =
            "Host=localhost;Port=5432;Database=catalog_schema_tests;Username=postgres;Password=postgres";

        [Fact]
        public void WriteContext_DeclaresTheWriteSchemaAsItsDefault()
        {
            //Arrange
            using var context = BuildWriteContext();

            //Act
            var defaultSchema = context.Model.GetDefaultSchema();

            //Assert
            Assert.Equal(DatabaseSchemas.Write, defaultSchema);
        }

        [Fact]
        public void ReadContext_DeclaresTheReadSchemaAsItsDefault()
        {
            //Arrange
            using var context = BuildReadContext();

            //Act
            var defaultSchema = context.Model.GetDefaultSchema();

            //Assert
            Assert.Equal(DatabaseSchemas.Read, defaultSchema);
        }

        [Fact]
        public void DefaultSchemas_AreDifferent()
        {
            //Arrange
            using var writeContext = BuildWriteContext();
            using var readContext = BuildReadContext();

            //Act
            var writeSchema = writeContext.Model.GetDefaultSchema();
            var readSchema = readContext.Model.GetDefaultSchema();

            //Assert
            // The whole point of the fix, stated as directly as it can be stated.
            Assert.NotEqual(writeSchema, readSchema);
        }

        [Fact]
        public void MappedTables_NoRelationIsClaimedByBothContexts()
        {
            //Arrange
            using var writeContext = BuildWriteContext();
            using var readContext = BuildReadContext();

            //Act
            var writeRelations = QualifiedTableNames(writeContext);
            var readRelations = QualifiedTableNames(readContext);
            var sharedRelations = writeRelations
                .Intersect(readRelations, StringComparer.Ordinal)
                .ToArray();

            //Assert
            // A shared entry means two migration histories CREATE the same relation and the deploy
            // step fails with 42P07. Nothing else in the suite can catch that.
            Assert.Empty(sharedRelations);
        }

        [Theory]
        [InlineData("Food")]
        [InlineData("Recipe")]
        [InlineData("MealPlan")]
        [InlineData("PlanDay")]
        [InlineData("MealTime")]
        public void TableSharedByName_IsMappedIntoADifferentSchemaOnEachSide(string tableName)
        {
            //Arrange
            using var writeContext = BuildWriteContext();
            using var readContext = BuildReadContext();

            //Act
            var writeRelation = QualifiedTableNames(writeContext).Single(x => x.EndsWith($".{tableName}", StringComparison.Ordinal));
            var readRelation = QualifiedTableNames(readContext).Single(x => x.EndsWith($".{tableName}", StringComparison.Ordinal));

            //Assert
            // Deliberately stronger than the empty-intersection test above: it names the five tables
            // that were actually duplicated, so a future change that renames one of them on one side
            // only stops the duplication instead of quietly making this test vacuous.
            Assert.Equal($"{DatabaseSchemas.Write}.{tableName}", writeRelation);
            Assert.Equal($"{DatabaseSchemas.Read}.{tableName}", readRelation);
        }

        [Fact]
        public void OutboxTable_StaysInTheOutboxSchemaAndNotInTheWriteSchema()
        {
            //Arrange
            using var context = BuildWriteContext();

            //Act
            var outboxTable = context.Model
                .GetRelationalModel()
                .Tables
                .Single(x => x.Name == "outboxMessage");

            //Assert
            // Joseco.Outbox.EFCore hardcodes ToTable("outboxMessage", "outbox") in an internal
            // configuration with no override, so this is an observation about the dependency, not
            // something the mapping can influence. It is asserted because it is the kind of thing a
            // library upgrade could silently change, and because the write context's own default
            // schema is the natural wrong answer.
            Assert.Equal(DatabaseSchemas.Outbox, outboxTable.Schema);
        }

        [Fact]
        public void WriteContext_ClaimsNoRelationInThePublicSchema()
        {
            //Arrange
            using var context = BuildWriteContext();

            //Act
            var unqualified = QualifiedTableNames(context)
                .Where(x => x.StartsWith("public.", StringComparison.Ordinal) || !x.Contains('.'))
                .ToArray();

            //Assert
            Assert.Empty(unqualified);
        }

        [Fact]
        public void ReadContext_ClaimsNoRelationInThePublicSchema()
        {
            //Arrange
            using var context = BuildReadContext();

            //Act
            var unqualified = QualifiedTableNames(context)
                .Where(x => x.StartsWith("public.", StringComparison.Ordinal) || !x.Contains('.'))
                .ToArray();

            //Assert
            Assert.Empty(unqualified);
        }

        private static DomainDbContext BuildWriteContext() =>
            new(new DbContextOptionsBuilder<DomainDbContext>()
                .UseNpgsql(DesignTimeConnectionString)
                .Options);

        private static StoredDbContext BuildReadContext() =>
            new(new DbContextOptionsBuilder<StoredDbContext>()
                .UseNpgsql(DesignTimeConnectionString)
                .Options);

        /// <summary>
        /// Every relation the context maps, as <c>schema.table</c>.
        /// </summary>
        /// <remarks>
        /// A table whose <see cref="ITable.Schema"/> is null inherits the model's default schema, so
        /// the fallback is applied here rather than left as an unqualified name that the two sides
        /// could both produce.
        /// </remarks>
        private static string[] QualifiedTableNames(DbContext context)
        {
            var defaultSchema = context.Model.GetDefaultSchema();

            return context.Model
                .GetRelationalModel()
                .Tables
                .Select(table => $"{table.Schema ?? defaultSchema}.{table.Name}")
                .ToArray();
        }
    }
}