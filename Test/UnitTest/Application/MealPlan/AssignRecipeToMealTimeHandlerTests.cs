using Catalog.Application.UseCase.Command.MealPlan.AssignRecipeToMealTime;
using Catalog.Domain;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Core;
using Xunit;

namespace Catalog.Tests.Application
{
    public class AssignRecipeToMealTimeHandlerTests
    {
        private readonly Mock<IMealPlanRepository> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<AssignRecipeToMealTimeHandler>> _loggerMock;
        private readonly AssignRecipeToMealTimeHandler _handler;

        public AssignRecipeToMealTimeHandlerTests()
        {
            _repoMock = new Mock<IMealPlanRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<AssignRecipeToMealTimeHandler>>();
            _handler = new AssignRecipeToMealTimeHandler(_repoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
        }

        private static MealPlan CreatePlanWithMealTime()
        {
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            return plan;
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessAndVerifiesInteractions()
        {
            // Arrange
            var plan = CreatePlanWithMealTime();
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            _repoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var recipeId = Guid.NewGuid();
            var command = new AssignRecipeToMealTimeCommand
            {
                MealPlanId = plan.Id,
                MealTimeId = mealTime.Id,
                Recipes = new List<RecipeItem> { new RecipeItem { RecipeId = recipeId, RationQuantity = 2 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("1 recipe(s) assigned successfully", result.Message);
            var assignment = Assert.Single(mealTime.AssignedRecipes);
            Assert.Equal(recipeId, assignment.RecipeId);
            _repoMock.Verify(r => r.FindByIdAsync(It.Is<Guid>(id => id == command.MealPlanId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Once);
        }

        [Fact]
        public async Task Handle_PlanNotFound_ReturnsFailureAndNoCommit()
        {
            // Arrange
            _repoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MealPlan?)null);
            var command = new AssignRecipeToMealTimeCommand
            {
                MealPlanId = Guid.NewGuid(),
                MealTimeId = Guid.NewGuid(),
                Recipes = new List<RecipeItem> { new RecipeItem { RecipeId = Guid.NewGuid(), RationQuantity = 1 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Meal plan not found", result.Message);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task Handle_TimeNotFoundInPlan_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var plan = CreatePlanWithMealTime();
            _repoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AssignRecipeToMealTimeCommand
            {
                MealPlanId = plan.Id,
                MealTimeId = Guid.NewGuid(),
                Recipes = new List<RecipeItem> { new RecipeItem { RecipeId = Guid.NewGuid(), RationQuantity = 1 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist in the plan", result.Message);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Handle_NonPositiveRation_ReturnsFailureAndNoCommit(int rationQuantity)
        {
            // Arrange
            var plan = CreatePlanWithMealTime();
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            _repoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AssignRecipeToMealTimeCommand
            {
                MealPlanId = plan.Id,
                MealTimeId = mealTime.Id,
                Recipes = new List<RecipeItem> { new RecipeItem { RecipeId = Guid.NewGuid(), RationQuantity = rationQuantity } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Ration must be greater than zero", result.Message);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateRecipeAssignment_ReturnsFailureAndNoCommit()
        {
            // Arrange
            var plan = CreatePlanWithMealTime();
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipeId = Guid.NewGuid();
            plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(1));
            _repoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(plan);
            var command = new AssignRecipeToMealTimeCommand
            {
                MealPlanId = plan.Id,
                MealTimeId = mealTime.Id,
                Recipes = new List<RecipeItem> { new RecipeItem { RecipeId = recipeId, RationQuantity = 1 } }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("is already assigned", result.Message);
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }
    }
}