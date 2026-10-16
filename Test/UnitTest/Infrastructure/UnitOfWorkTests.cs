using Catalog.Application;
using Catalog.Domain.Event.Recipe;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.DomainModel;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Infrastructure
{
    public class UnitOfWorkTests
    {
        private readonly Mock<IMediator> _mediatorMock;

        public UnitOfWorkTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task CommitAsync_WithAggregates_SavesAggregatesAndStagesOutboxRows()
        {
            // Arrange
            using var provider = BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");

            context.Recipes.Add(recipe);

            // Act
            await unitOfWork.CommitAsync();

            // Assert
            context.ChangeTracker.Clear();
            var outboxMessages = await context.OutboxMessages.ToListAsync();

            Assert.Equal(1, await context.Recipes.CountAsync());
            Assert.Single(outboxMessages);
            Assert.False(outboxMessages[0].Processed);
            Assert.IsType<RecipeCreated>(outboxMessages[0].Content);
        }

        [Fact]
        public async Task CommitAsync_WhenSaveChangesFails_PersistsNothing()
        {
            // Arrange
            using var provider = BuildServiceProvider(failOnSave: true);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");

            context.Recipes.Add(recipe);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());

            // Assert
            Assert.Empty(context.ChangeTracker.Entries());
            Assert.Equal(0, await context.Recipes.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
        }

        [Fact]
        public async Task CommitAsync_CalledTwice_DoesNotReenter()
        {
            // Arrange
            var context = new CountingDomainDbContext(BuildOptions());
            var unitOfWork = new UnitOfWork(context, _mediatorMock.Object);
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");

            context.Recipes.Add(recipe);

            _mediatorMock
                .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
                .Returns(async (INotification notification, CancellationToken cancellationToken) =>
                {
                    await unitOfWork.CommitAsync(cancellationToken);
                });

            // Act
            await unitOfWork.CommitAsync();

            // Assert
            _mediatorMock.Verify(
                x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.Equal(1, context.SaveChangesCallCount);
        }

        [Fact]
        public async Task CollectDomainEvents_ClearsEvents()
        {
            // Arrange
            using var provider = BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");

            context.Recipes.Add(recipe);

            // Act
            await unitOfWork.CommitAsync();

            // Assert
            Assert.Empty(recipe.DomainEvents);
        }

        [Fact]
        public async Task CommitAsync_WithClearedEvents_DoesNotStageDuplicateOutboxRows()
        {
            // Arrange
            using var provider = BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var recipe = new Recipe(new RecipeName("Ensalada de frutas"), "Picar y mezclar.");

            context.Recipes.Add(recipe);
            await unitOfWork.CommitAsync();

            // Act
            await unitOfWork.CommitAsync();

            // Assert
            context.ChangeTracker.Clear();
            Assert.Equal(1, await context.OutboxMessages.CountAsync());
        }

        [Fact]
        public void GetOutboxMessages_ReturnsTheDomainDbContextOutboxSet()
        {
            // Arrange
            using var provider = BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
            var outboxDatabase = scope.ServiceProvider.GetRequiredService<IOutboxDatabase<DomainEvent>>();

            // Act
            var outboxMessages = outboxDatabase.GetOutboxMessages();

            // Assert
            Assert.Same(context.OutboxMessages, outboxMessages);
        }

        private static ServiceProvider BuildServiceProvider(bool failOnSave = false)
        {
            var options = BuildOptions();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();

            services.AddScoped<DomainDbContext>(_ =>
                failOnSave ? new FailingDomainDbContext(options) : new DomainDbContext(options));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutboxDatabase<DomainEvent>, UnitOfWork>();
            services.AddScoped<IOutboxService<DomainEvent>, EfCoreOutboxService<DomainEvent>>();

            return services.BuildServiceProvider();
        }

        private static DbContextOptions<DomainDbContext> BuildOptions()
        {
            return new DbContextOptionsBuilder<DomainDbContext>()
                .UseInMemoryDatabase($"UnitOfWorkTests_{Guid.NewGuid()}")
                .Options;
        }

        private sealed class CountingDomainDbContext : DomainDbContext
        {
            public CountingDomainDbContext(DbContextOptions<DomainDbContext> options) : base(options)
            {
            }

            public int SaveChangesCallCount { get; private set; }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesCallCount++;

                return base.SaveChangesAsync(cancellationToken);
            }
        }

        private sealed class FailingDomainDbContext : DomainDbContext
        {
            public FailingDomainDbContext(DbContextOptions<DomainDbContext> options) : base(options)
            {
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Simulated persistence failure.");
            }
        }
    }
}