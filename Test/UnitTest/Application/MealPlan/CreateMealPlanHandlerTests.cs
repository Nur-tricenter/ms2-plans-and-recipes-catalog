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
            _handler = new CreateMealPlanHandler(_repoMock.Object, _factoryMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>())).Returns(plan);
            var command = new CreateMealPlanCommand { Name = "Plan Saludable", DurationType = "FIFTEEN_DAYS", MealsPerDay = 3 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(plan.Id, result.Value);
            Assert.Equal("Meal plan created successfully", result.Message);
            _factoryMock.Verify(f => f.Create("Plan Saludable", It.Is<PlanDuration>(d => d.Type == DurationType.FIFTEEN_DAYS), 3), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.Is<MealPlan>(p => p.Name.Name == "Plan Saludable")), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("SEMANAL")]
        [InlineData("")]
        [InlineData(null)]
        public async Task Handle_InvalidDurationType_ReturnsFailureAndNoCommit(string? durationType)
        {
            // Arrange
            var command = new CreateMealPlanCommand { Name = "Plan", DurationType = durationType!, MealsPerDay = 3 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            _factoryMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>()), Times.Never);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<MealPlan>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Handle_InvalidName_FactoryThrows_ReturnsFailureAndNoCommit(string? name)
        {
            // Arrange
            _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<PlanDuration>(), It.IsAny<int>()))
                .Throws(new BussinessRuleValidationException("Plan name cannot be null"));
            var command = new CreateMealPlanCommand { Name = name!, DurationType = "FIFTEEN_DAYS", MealsPerDay = 3 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Plan name cannot be null", result.Message);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<MealPlan>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
