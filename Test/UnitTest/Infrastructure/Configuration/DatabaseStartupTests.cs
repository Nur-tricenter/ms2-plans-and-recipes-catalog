using Catalog.Infrastructure.Configuration;
using Catalog.Infrastructure.Seed;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure.Configuration
{
    /// <summary>
    /// Covers the two independent gates in <see cref="DatabaseStartup"/>: migrating is not seeding,
    /// and each one is decided by its own configuration key rather than by whatever the host happens
    /// to be.
    /// </summary>
    /// <remarks>
    /// This is where the production seeding bug is pinned down. The old
    /// <c>if (context.Database.IsRelational()) { Migrate(); Initialize(); }</c> block answered a
    /// question about the DATABASE PROVIDER when the question was about INTENT — and since
    /// <c>IsRelational()</c> is true under Npgsql, production seeded 30 foods, 25 recipes and 2
    /// meal plans on every single boot.
    /// </remarks>
    public class DatabaseStartupTests
    {
        private readonly Mock<IDatabaseMigrator> _migratorMock = new();
        private readonly Mock<IDbInitializer> _initializerMock = new();
        private readonly RecordingLogger<DatabaseStartup> _logger = new();

        [Fact]
        public async Task RunAsync_ByDefault_DoesNotMigrateAndDoesNotSeed()
        {
            //Arrange
            var sut = BuildSut(new MigrationsOptions(), new SeedOptions());

            //Act
            await sut.RunAsync();

            //Assert
            // The safe default, asserted against the real option defaults rather than against
            // hand-picked values, so renaming or re-defaulting a flag breaks this test.
            _migratorMock.Verify(m => m.MigrateAsync(It.IsAny<CancellationToken>()), Times.Never);
            _initializerMock.Verify(i => i.Initialize(), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenApplyOnStartupIsFalse_DoesNotMigrate()
        {
            //Arrange
            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = false },
                new SeedOptions { Enabled = false });

            //Act
            await sut.RunAsync();

            //Assert
            _migratorMock.Verify(m => m.MigrateAsync(It.IsAny<CancellationToken>()), Times.Never);
            _initializerMock.Verify(i => i.Initialize(), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenApplyOnStartupIsTrue_Migrates()
        {
            //Arrange
            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = true },
                new SeedOptions { Enabled = false });

            //Act
            await sut.RunAsync();

            //Assert
            _migratorMock.Verify(m => m.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenSeedIsDisabled_DoesNotSeed()
        {
            //Arrange
            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = false },
                new SeedOptions { Enabled = false });

            //Act
            await sut.RunAsync();

            //Assert
            _initializerMock.Verify(i => i.Initialize(), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenSeedIsEnabled_Seeds()
        {
            //Arrange
            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = false },
                new SeedOptions { Enabled = true });

            //Act
            await sut.RunAsync();

            //Assert
            _initializerMock.Verify(i => i.Initialize(), Times.Once);
            _migratorMock.Verify(m => m.MigrateAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenBothAreEnabled_MigratesBeforeSeeding()
        {
            //Arrange
            // Seeding reads the write side (DbInitializer queries Foods and Recipes through
            // DomainDbContext), so a host that seeded first would hit the exact 42P01 this work set
            // out to fix. The order is recorded rather than asserted through Moq's sequence helpers,
            // which were removed from the 4.x line: a plain list of what happened, in order, states
            // the same contract and cannot be broken by a Moq upgrade.
            var callOrder = new List<string>();

            _migratorMock.Setup(m => m.MigrateAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("migrate"))
                .Returns(Task.CompletedTask);
            _initializerMock.Setup(i => i.Initialize())
                .Callback(() => callOrder.Add("seed"))
                .Returns(Task.CompletedTask);

            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = true },
                new SeedOptions { Enabled = true });

            //Act
            await sut.RunAsync();

            //Assert
            Assert.Equal(new[] { "migrate", "seed" }, callOrder);
        }

        [Fact]
        public async Task RunAsync_WhenMigrationFails_PropagatesTheFailureAndNeverSeeds()
        {
            //Arrange
            var failure = new InvalidOperationException("relation \"foods\" does not exist");
            _migratorMock.Setup(m => m.MigrateAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);

            var sut = BuildSut(
                new MigrationsOptions { ApplyOnStartup = true },
                new SeedOptions { Enabled = true });

            //Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync());

            //Assert
            // Not swallowed: a boot that reports success after a failed migration is worse than one
            // that crashes. And the seed genuinely never runs as a side effect of migrating.
            Assert.Same(failure, thrown);
            _initializerMock.Verify(i => i.Initialize(), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenBothAreDisabled_LogsBothSkipsAndWhatToRunInstead()
        {
            //Arrange
            var sut = BuildSut(new MigrationsOptions(), new SeedOptions());

            //Act
            await sut.RunAsync();

            //Assert
            // A silent skip is indistinguishable from a silent no-op, so both gates say what they
            // skipped and, for the schema, which command does it instead.
            Assert.Contains(_logger.Messages, message =>
                message.Contains($"{MigrationsOptions.SectionName}:{nameof(MigrationsOptions.ApplyOnStartup)}") &&
                message.Contains("--migrate"));
            Assert.Contains(_logger.Messages, message =>
                message.Contains($"{SeedOptions.SectionName}:{nameof(SeedOptions.Enabled)}"));
        }

        [Fact]
        public void SeedOptions_ByDefault_IsDisabled()
        {
            //Arrange
            var options = new SeedOptions();

            //Assert
            Assert.Equal(SeedOptions.EnabledDefault, options.Enabled);
            Assert.False(options.Enabled);
        }

        private DatabaseStartup BuildSut(MigrationsOptions migrations, SeedOptions seed) => new(
            _migratorMock.Object,
            _initializerMock.Object,
            Options.Create(migrations),
            Options.Create(seed),
            _logger);
    }
}
