using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Persistence.DomainModel;
using Catalog.Infrastructure.Persistence.StoredModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catalog.Tests.Infrastructure.Configuration
{
    /// <summary>
    /// Covers <see cref="DatabaseMigrator"/> against the only database provider this harness can
    /// honestly provide: EF Core InMemory.
    /// </summary>
    /// <remarks>
    /// That sounds like a weak remit and it is a deliberate one. A relational provider is not
    /// available in the suite (no live PostgreSQL, and SQLite cannot create these migrations'
    /// PostgreSQL-specific types), so the properties that ARE testable without one are asserted here
    /// and the rest is argued in the type's remarks: that both contexts are covered in a defined
    /// order, that a non-relational provider is a no-op rather than the
    /// <c>InvalidOperationException</c> that <c>Migrate()</c> throws under InMemory, and that
    /// running twice is the same as running once. Failure propagation is covered where it can be
    /// observed without a database, in <see cref="DatabaseStartupTests"/>.
    /// <para>
    /// The regression this file guards is specific and was real: <c>Startup.Configure</c> used to
    /// call <c>Migrate()</c> on <see cref="StoredDbContext"/> only, gated on <c>IsRelational()</c>,
    /// which meant <see cref="DomainDbContext"/> — the context the service writes through, and the
    /// one that owns the outbox — was never migrated at all.
    /// </para>
    /// </remarks>
    public class DatabaseMigratorTests
    {
        private readonly RecordingLogger<DatabaseMigrator> _logger = new();

        [Fact]
        public async Task MigrateAsync_WithNonRelationalProviders_DoesNotThrow()
        {
            //Arrange
            var sut = BuildSut();

            //Act
            // Migrate() under the InMemory provider throws
            // "Relational-specific methods can only be used when the context is using a relational
            // database provider". Not throwing is the assertion.
            var exception = await Record.ExceptionAsync(() => sut.MigrateAsync());

            //Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task MigrateAsync_WithNonRelationalProviders_SkipsBothContextsAndSaysWhy()
        {
            //Arrange
            var sut = BuildSut();

            //Act
            await sut.MigrateAsync();

            //Assert
            // Both contexts, each named, each with the provider that disqualified it. A migrator that
            // silently did nothing is indistinguishable from one that crashed halfway through.
            Assert.Contains(_logger.Messages, message =>
                message.Contains(DatabaseMigrator.WriteSide) &&
                message.Contains("is not relational") &&
                message.Contains("Microsoft.EntityFrameworkCore.InMemory"));
            Assert.Contains(_logger.Messages, message =>
                message.Contains(DatabaseMigrator.ReadSide) &&
                message.Contains("is not relational") &&
                message.Contains("Microsoft.EntityFrameworkCore.InMemory"));
        }

        [Fact]
        public async Task MigrateAsync_WithNonRelationalContexts_DoesNotClaimToHaveAppliedAnything()
        {
            //Arrange
            var sut = BuildSut();

            //Act
            await sut.MigrateAsync();

            //Assert
            Assert.DoesNotContain(_logger.Messages, message => message.Contains("pending migration(s) to"));
            Assert.Contains(_logger.Messages, message => message.Contains("Database migrations finished."));
        }

        [Fact]
        public async Task MigrateAsync_WhenRunTwice_DoesTheSameWorkAndAppliesNothingOnEitherPass()
        {
            //Arrange
            var sut = BuildSut();
            await sut.MigrateAsync();
            var afterFirstRun = _logger.Messages.ToArray();

            //Act
            await sut.MigrateAsync();

            //Assert
            // Idempotency: EF Core keys __EFMigrationsHistory by migration id and applies only what
            // is missing, so a second pass is indistinguishable from a first. The observable form of
            // that here is "the second run took exactly as many steps as the first and again claimed
            // to apply nothing", which is what the log is for.
            var secondRunMessages = _logger.Messages.Skip(afterFirstRun.Length).ToArray();

            Assert.Equal(afterFirstRun.Length, secondRunMessages.Length);
            Assert.DoesNotContain(secondRunMessages, message => message.Contains("pending migration(s) to"));
            Assert.Contains(secondRunMessages, message => message.Contains(DatabaseMigrator.WriteSide));
            Assert.Contains(secondRunMessages, message => message.Contains(DatabaseMigrator.ReadSide));
        }

        [Fact]
        public void MigrationsOptions_ByDefault_DoesNotApplyOnStartupAndBudgetsTheRun()
        {
            //Arrange
            var options = new MigrationsOptions();

            //Assert
            Assert.Equal(MigrationsOptions.ApplyOnStartupDefault, options.ApplyOnStartup);
            Assert.False(options.ApplyOnStartup);
            Assert.True(options.TimeoutSeconds > 0);
        }

        private DatabaseMigrator BuildSut() => new(
            new DomainDbContext(new DbContextOptionsBuilder<DomainDbContext>()
                .UseInMemoryDatabase($"MigratorDomain_{Guid.NewGuid():N}").Options),
            new StoredDbContext(new DbContextOptionsBuilder<StoredDbContext>()
                .UseInMemoryDatabase($"MigratorStored_{Guid.NewGuid():N}").Options),
            Options.Create(new MigrationsOptions()),
            _logger);
    }
}
