using Catalog.Application.UseCase.Command.MealPlan.AddMealTime;
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
    public class AddMealTimeHandlerTests
    {
        private readonly Mock<IMealPlanRepository> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<AddMealTimeHandler>> _loggerMock;
        private readonly AddMealTimeHandler _handler;

        public AddMealTimeHandlerTests()
        {
            _repoMock = new Mock<IMealPlanRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<AddMealTimeHandler>>();
            _handler = new AddMealTimeHandler(_repoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        private static MealPlan CreatePlan() =>
            new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var plan = CreatePlan();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AddMealTimeCommand { MealPlanId = plan.Id, DayNumber = 1, MealTimeType = (int)MealTimeType.Breakfast };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Meal time added successfully", result.Message);
            var mealTime = Assert.Single(plan.PlanDays.First(d => d.DayNumber == 1).MealTimes);
            Assert.Equal("Breakfast", mealTime.Name);
            _repoMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id == command.MealPlanId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_PlanNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MealPlan?)null);
            var command = new AddMealTimeCommand { MealPlanId = Guid.NewGuid(), DayNumber = 1, MealTimeType = (int)MealTimeType.Breakfast };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Meal plan not found", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(99)]
        public async Task Handle_InvalidMealTimeType_ReturnsFailureAndNoCommit(int type)
        {
            // Arrange
            var plan = CreatePlan();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AddMealTimeCommand { MealPlanId = plan.Id, DayNumber = 1, MealTimeType = type };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid meal time type", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DayNotInPlan_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var plan = CreatePlan();
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AddMealTimeCommand { MealPlanId = plan.Id, DayNumber = 16, MealTimeType = (int)MealTimeType.Breakfast };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Day 16 does not exist in the plan", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateMealTime_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var plan = CreatePlan();
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AddMealTimeCommand { MealPlanId = plan.Id, DayNumber = 1, MealTimeType = (int)MealTimeType.Breakfast };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Meal time with order 1 already exists on day", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}