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
        [Theory]
        [InlineData("PlanDay", "mealPlanId")]
        [InlineData("MealTime", "planDayId")]
        [InlineData("RecipeAssignment", "mealTimeId")]
        public void ShadowForeignKey_IsMappedToANonNullableCamelCaseColumn(
            string dependentTable,
            string expectedColumn)
        {
            //Arrange
            using var context = BuildWriteContext();

            //Act
            var table = context.Model
                .GetRelationalModel()
                .Tables
                .Single(x => x.Name == dependentTable);
            var column = Assert.Single(table.Columns, x => x.Name == expectedColumn);

            //Assert
            // These three foreign keys have no property on the domain entity, so EF discovers
            // them as shadow properties. Left to its own conventions EF names the property and
            // the column "MealPlanId" / "PlanDayId" / "MealTimeId" and leaves the first two
            // nullable, which contradicted every explicitly camelCase column around them and
            // disagreed with the read side, where the same three keys are required camelCase
            // columns. A PlanDay always belongs to a MealPlan and a MealTime always belongs to
            // a PlanDay, so NOT NULL is also the truthful modelling.
            Assert.False(column.IsNullable);

            // Asserted at the relational level on purpose: the entity type names for owned
            // navigations do not match the CLR type name, so FindEntityType cannot be used to
            // locate these and would fail for a reason unrelated to what is being asserted.
        }

        [Theory]
        [InlineData("PlanDay", "MealPlanId")]
        [InlineData("MealTime", "PlanDayId")]
        [InlineData("RecipeAssignment", "MealTimeId")]
        public void ShadowForeignKey_NoLongerLeaksThePascalCaseConventionIntoTheSchema(
            string dependentTable,
            string shadowPropertyName)
        {
            //Arrange
            using var context = BuildWriteContext();

            //Act
            var table = context.Model
                .GetRelationalModel()
                .Tables
                .Single(x => x.Name == dependentTable);
            var columns = table.Columns.Select(x => x.Name).ToArray();

            //Assert
            Assert.DoesNotContain(shadowPropertyName, columns);
        }

        [Fact]
        public void AssignmentChild_IsMappedUnderTheSameNameOnBothSides()
        {
            //Arrange
            using var writeContext = BuildWriteContext();
            using var readContext = BuildReadContext();

            //Act
            var writeRelations = QualifiedTableNames(writeContext);
            var readRelations = QualifiedTableNames(readContext);

            //Assert
            // The domain calls this ValueObject RecipeAssignment and the write table agreed. The
            // read side called the same concept AssignedRecipe, so searching for the domain name
            // missed half the model — and in a CQRS codebase, following an event from the
            // aggregate to its projection is the main maintenance task there is.
            Assert.Contains($"{DatabaseSchemas.Write}.RecipeAssignment", writeRelations);
            Assert.Contains($"{DatabaseSchemas.Read}.RecipeAssignment", readRelations);
        }

        [Fact]
        public void NoColumnOnEitherSide_StartsWithAnUppercaseLetter()
        {
            //Arrange
            using var writeContext = BuildWriteContext();
            using var readContext = BuildReadContext();

            //Act
            var offending = ColumnNames(writeContext)
                .Concat(ColumnNames(readContext))
                .Where(x => x.Length > 0 && char.IsUpper(x[0]))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            //Assert
            // camelCase is this project's column convention, and it is not a free choice: the
            // outbox table belongs to Joseco.Outbox.EFCore, which hardcodes its own camelCase
            // columns (outboxId, processedOn, correlationId, traceId, spanId), so the schema
            // already contains camelCase this project does not own. Leaving three shadow
            // foreign keys and two owned keys on the EF default meant one schema held two
            // conventions side by side, and nothing in the suite could see it.
            Assert.Empty(offending);
        }

        private static string[] QualifiedTableNames(DbContext context)
        {
            var defaultSchema = context.Model.GetDefaultSchema();

            return context.Model
                .GetRelationalModel()
                .Tables
                .Select(table => $"{table.Schema ?? defaultSchema}.{table.Name}")
                .ToArray();
        }

        /// <summary>
        /// Every column name the context maps, qualified as <c>schema.table.column</c>.
        /// </summary>
        /// <remarks>
        /// Qualifying with the table is what makes a failure actionable: a bare column name is
        /// ambiguous when the same name appears in both schemas, which is the normal case here.
        /// </remarks>
        private static string[] ColumnNames(DbContext context)
        {
            var defaultSchema = context.Model.GetDefaultSchema();

            return context.Model
                .GetRelationalModel()
                .Tables
                .SelectMany(table => table.Columns.Select(column =>
                    $"{table.Schema ?? defaultSchema}.{table.Name}.{column.Name}"))
                .ToArray();
        }
    }
}