using Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan;
using Catalog.Domain.Primitives;
using Catalog.Domain;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Application
{
    public class CreateMealPlanHandlerTests
    {
        private readonly Mock<IMealPlanRepository> _repoMock;
        private readonly Mock<IMealPlanFactory> _factoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CreateMealPlanHandler>> _loggerMock;
        private readonly CreateMealPlanHandler _handler;

        public CreateMealPlanHandlerTests()
        {
            _repoMock = new Mock<IMealPlanRepository>();
            _factoryMock = new Mock<IMealPlanFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateMealPlanHandler>>();

            _handler = new CreateMealPlanHandler(
                _repoMock.Object,
                _factoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            var authorId = Guid.NewGuid();
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3, authorId);
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>(), It.IsAny<Guid>())).Returns(plan);

            var command = new CreateMealPlanCommand
            {
                Name = "Plan Saludable",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 3,
                AuthorIdFromToken = authorId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(plan.Id, result.Value);
            Assert.Equal("Meal plan created successfully", result.Message);
            _factoryMock.Verify(f => f.Create("Plan Saludable", It.Is<PlanDuration>(d => d.Type == DurationType.FIFTEEN_DAYS), 3, authorId), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.Is<MealPlan>(p => p.Name.Name == "Plan Saludable")), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmptyAuthor_FailsWithoutReachingTheFactory()
        {
            // The author comes from the validated bearer token, so an empty id means the caller is
            // not authenticated. The handler must refuse it itself rather than letting the factory
            // throw from inside the aggregate.
            var command = new CreateMealPlanCommand
            {
                Name = "Plan Saludable",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 3,
                AuthorIdFromToken = Guid.Empty
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            _factoryMock.Verify(
                f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>(), It.IsAny<Guid>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CreatesThePlanAgainstTheAuthenticatedAccount()
        {
            // The plan's author must be the account from the token, never anything the caller
            // supplied: a request that claims a different author is ignored, because the command
            // carries one id and it comes from the credential.
            var tokenAccount = Guid.NewGuid();
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3, tokenAccount);
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>(), tokenAccount)).Returns(plan);

            await _handler.Handle(new CreateMealPlanCommand
            {
                Name = "Plan Saludable",
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 3,
                AuthorIdFromToken = tokenAccount
            }, CancellationToken.None);

            _repoMock.Verify(
                r => r.AddAsync(It.Is<MealPlan>(p => HasAuthor(p, tokenAccount))),
                Times.Once);
        }

        /// <summary>
        /// Whether the plan carries the given author.
        /// </summary>
        /// <remarks>
        /// A named method rather than an inline expression because Moq builds an expression tree,
        /// and chained null-forgiving accessors on a nullable value type do not survive being
        /// rewritten inside one.
        /// </remarks>
        private static bool HasAuthor(MealPlan plan, Guid authorId)
        {
            PlanAuthorId? author = plan.AuthorId;
            return author != null && author.Value == authorId;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NO_SUCH_DURATION")]
        public async Task Handle_InvalidDurationType_ReturnsFailureAndNoCommit(string? durationType)
        {
            var command = new CreateMealPlanCommand
            {
                Name = "Plan Saludable",
                DurationType = durationType!,
                MealsPerDay = 3,
                AuthorIdFromToken = Guid.NewGuid()
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Handle_InvalidName_FactoryThrows_ReturnsFailureAndNoCommit(string? name)
        {
            _factoryMock
                .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>(), It.IsAny<Guid>()))
                .Throws(new BussinessRuleValidationException("Invalid plan name"));

            var command = new CreateMealPlanCommand
            {
                Name = name!,
                DurationType = "FIFTEEN_DAYS",
                MealsPerDay = 3,
                AuthorIdFromToken = Guid.NewGuid()
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsFailed);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}