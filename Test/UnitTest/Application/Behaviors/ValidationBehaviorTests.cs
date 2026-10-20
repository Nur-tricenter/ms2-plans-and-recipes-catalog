using Catalog.Application.Abstractions;
using Catalog.Application.Behaviors;
using Catalog.Application.Utils;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Catalog.Tests.Application
{
    public class ValidationBehaviorTests
    {
        private const string EmptyListMessage = "At least one item must be supplied";

        [Fact]
        public async Task Handle_ValidRequest_InvokesNextAndReturnsResultUnchanged()
        {
            // Arrange
            var request = new TestCommand { Items = new List<string> { "one" } };
            var sut = new ValidationBehavior<TestCommand, Result>();
            var expected = Result.Ok("handled");
            var nextInvoked = false;

            // Act
            var result = await sut.Handle(request, _ =>
            {
                nextInvoked = true;
                return Task.FromResult(expected);
            }, CancellationToken.None);

            // Assert
            Assert.Same(expected, result);
            Assert.True(nextInvoked);
        }

        [Fact]
        public async Task Handle_InvalidRequest_DoesNotInvokeNext()
        {
            // Arrange
            var request = new TestCommand { Items = new List<string>() };
            var sut = new ValidationBehavior<TestCommand, Result>();
            var nextInvoked = false;

            // Act
            await sut.Handle(request, _ =>
            {
                nextInvoked = true;
                return Task.FromResult(Result.Ok());
            }, CancellationToken.None);

            // Assert
            Assert.False(nextInvoked);
        }

        [Fact]
        public async Task Handle_InvalidRequest_ReturnsFailureWithAttributeErrorMessage()
        {
            // Arrange
            var request = new TestCommand { Items = new List<string>() };
            var sut = new ValidationBehavior<TestCommand, Result>();

            // Act
            var result = await sut.Handle(request, _ => Task.FromResult(Result.Ok()), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Equal(EmptyListMessage, result.Message);
        }

        [Fact]
        public async Task Handle_GenericCommandRequest_Invalid_ReturnsFailure()
        {
            // Arrange
            var request = new TestGenericCommand { Items = new List<string>() };
            var sut = new ValidationBehavior<TestGenericCommand, Result<Guid>>();

            // Act
            var result = await sut.Handle(request, _ => Task.FromResult(Result.Ok(Guid.NewGuid())), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailed);
            Assert.Equal(EmptyListMessage, result.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task Handle_VariedItemCounts_AlwaysInvokesNext(int itemCount)
        {
            // Arrange
            var request = new TestCommand
            {
                Items = Enumerable.Range(0, itemCount).Select(i => i.ToString()).ToList()
            };
            var sut = new ValidationBehavior<TestCommand, Result>();
            var nextInvoked = false;

            // Act
            var result = await sut.Handle(request, _ =>
            {
                nextInvoked = true;
                return Task.FromResult(Result.Ok());
            }, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(nextInvoked);
        }

        [Fact]
        public void Resolve_ForNonGenericCommand_ReturnsValidationBehavior()
        {
            // Arrange
            var provider = BuildProvider();

            // Act
            var behaviors = provider.GetServices<IPipelineBehavior<TestNonGenericCommand, Result>>();

            // Assert
            var behavior = Assert.Single(behaviors);
            Assert.IsType<ValidationBehavior<TestNonGenericCommand, Result>>(behavior);
        }

        [Fact]
        public void Resolve_ForQueryRequest_ExcludesValidationBehavior()
        {
            // Arrange
            var provider = BuildProvider();

            // Act
            var behaviors = provider.GetServices<IPipelineBehavior<TestQuery, Result>>();

            // Assert
            Assert.Empty(behaviors);
        }

        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Catalog.Application.Extensions).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services.BuildServiceProvider();
        }

        private sealed class TestCommand : ICommand<Result>
        {
            [MinLength(1, ErrorMessage = EmptyListMessage)]
            public List<string> Items { get; set; } = new();
        }

        private sealed class TestGenericCommand : ICommand<Result<Guid>>
        {
            [MinLength(1, ErrorMessage = EmptyListMessage)]
            public List<string> Items { get; set; } = new();
        }

        private sealed class TestNonGenericCommand : ICommand
        {
            [MinLength(1, ErrorMessage = EmptyListMessage)]
            public List<string> Items { get; set; } = new();
        }

        private sealed class TestQuery : IQuery<Result>
        {
            [MinLength(1, ErrorMessage = EmptyListMessage)]
            public List<string> Items { get; set; } = new();
        }
    }
}