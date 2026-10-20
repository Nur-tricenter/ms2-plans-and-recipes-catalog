using Catalog.Application.UseCase.Command.MealPlan.RemoveRecipeFromMealTime;
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
    public class RemoveRecipeFromMealTimeHandlerTests
    {
        private readonly Mock<IMealPlanRepository> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<RemoveRecipeFromMealTimeHandler>> _loggerMock;
        private readonly RemoveRecipeFromMealTimeHandler _handler;

        public RemoveRecipeFromMealTimeHandlerTests()
        {
            _repoMock = new Mock<IMealPlanRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<RemoveRecipeFromMealTimeHandler>>();
            _handler = new RemoveRecipeFromMealTimeHandler(_repoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipeId = Guid.NewGuid();
            plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(1));
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(plan);
            var command = new RemoveRecipeFromMealTimeCommand { MealPlanId = plan.Id, MealTimeId = mealTime.Id, RecipeId = recipeId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Recipe removed from meal time successfully", result.Message);
            Assert.Empty(mealTime.AssignedRecipes);
            _repoMock.Verify(r => r.GetByIdAsync(It.Is<Guid>(id => id == command.MealPlanId), It.IsAny<bool>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_PlanNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((MealPlan?)null);
            var command = new RemoveRecipeFromMealTimeCommand { MealPlanId = Guid.NewGuid(), MealTimeId = Guid.NewGuid(), RecipeId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Meal plan not found", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TimeNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(plan);
            var command = new RemoveRecipeFromMealTimeCommand { MealPlanId = plan.Id, MealTimeId = Guid.NewGuid(), RecipeId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist in the plan", result.Message);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
