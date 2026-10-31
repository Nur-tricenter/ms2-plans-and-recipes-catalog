using Nur.Tricenter.Integration.Messages.Catalog;
using Catalog.Application.UseCase.DomainEventHandler;
using Catalog.Domain;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Catalog.Tests.Application
{
    public class PublishIntegrationEventWhenMealPlanCompositionChangedTests
    {
        private readonly Mock<IExternalPublisher> _publisherMock = new();
        private readonly Mock<IMealPlanRepository> _plansMock = new();
        private readonly Mock<IRecipeRepository> _recipesMock = new();
        private readonly List<MealPlanCompositionChanged> _published = new();
        private readonly PublishIntegrationEventWhenMealPlanCompositionChanged _handler;

        public PublishIntegrationEventWhenMealPlanCompositionChangedTests()
        {
            _publisherMock
                .Setup(x => x.PublishAsync(It.IsAny<MealPlanCompositionChanged>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .Callback<MealPlanCompositionChanged, string?, bool>((message, _, _) => _published.Add(message))
                .Returns(Task.CompletedTask);
            _handler = new PublishIntegrationEventWhenMealPlanCompositionChanged(
                new Mock<ILogger<PublishIntegrationEventWhenMealPlanCompositionChanged>>().Object,
                _publisherMock.Object,
                _plansMock.Object,
                _recipesMock.Object);
        }

        private (MealPlan Plan, Guid MealTimeId, Recipe Recipe) ComposedPlan()
        {
            var plan = new MealPlanFactory().Create("Plan Saludable", new PlanDuration(DurationType.FIFTEEN_DAYS), 3, Guid.NewGuid());
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipe = new Recipe(new RecipeName("Ensalada"), "Mezclar");
            plan.AssignRecipeToMealTime(mealTime.Id, recipe.Id, new Ration(2));

            _plansMock.Setup(r => r.GetByIdAsync(plan.Id, true)).ReturnsAsync(plan);
            _recipesMock.Setup(r => r.GetByIdAsync(recipe.Id, true)).ReturnsAsync(recipe);
            return (plan, mealTime.Id, recipe);
        }

        [Fact]
        public async Task Handle_RecipeAssigned_PublishesTheFullPlanSnapshot()
        {
            // Arrange
            var (plan, mealTimeId, recipe) = ComposedPlan();
            var notification = new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(plan.Id, 1, mealTimeId, recipe.Id, 2));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var message = Assert.Single(_published);
            Assert.Equal(plan.Id, message.PlanId);
            Assert.Equal(15, message.Days.Count);
            var day = message.Days.Single(d => d.DayNumber == 1);
            var mealTime = Assert.Single(day.MealTimes);
            Assert.Equal("Breakfast", mealTime.Name);
            var published = Assert.Single(mealTime.Recipes);
            Assert.Equal(recipe.Id, published.RecipeId);
            Assert.Equal("Ensalada", published.RecipeName);
            Assert.Equal(2, published.Rations);
            Assert.Empty(message.Days.Single(d => d.DayNumber == 2).MealTimes);
        }

        [Fact]
        public async Task Handle_PublishesToCompositionDestinationDeclaringIt()
        {
            // Arrange
            var (plan, mealTimeId, _) = ComposedPlan();

            // Act
            await _handler.Handle(
                new OutboxMessage<MealTimeAddedToPlan>(new MealTimeAddedToPlan(plan.Id, 1, mealTimeId, "Breakfast", 0)),
                CancellationToken.None);

            // Assert
            _publisherMock.Verify(
                x => x.PublishAsync(It.IsAny<MealPlanCompositionChanged>(), "meal-plan-composition-changed", true),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RecipeRemoved_PublishesTheSnapshotWithoutIt()
        {
            // Arrange
            var (plan, mealTimeId, recipe) = ComposedPlan();
            plan.RemoveRecipeFromMealTime(mealTimeId, recipe.Id);

            // Act
            await _handler.Handle(
                new OutboxMessage<RecipeRemovedFromMealTime>(new RecipeRemovedFromMealTime(plan.Id, 1, mealTimeId, recipe.Id)),
                CancellationToken.None);

            // Assert
            var message = Assert.Single(_published);
            Assert.Empty(Assert.Single(message.Days.Single(d => d.DayNumber == 1).MealTimes).Recipes);
        }

        [Fact]
        public async Task Handle_PropagatesCorrelationIdAndSource()
        {
            // Arrange
            var (plan, mealTimeId, recipe) = ComposedPlan();
            var notification = new OutboxMessage<RecipeAssignedToMealTime>(
                new RecipeAssignedToMealTime(plan.Id, 1, mealTimeId, recipe.Id, 2), "corr-1", "trace-1", "span-1");

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            var message = Assert.Single(_published);
            Assert.Equal("corr-1", message.CorrelationId);
            Assert.Equal("catalog", message.Source);
        }

        [Fact]
        public async Task Handle_PlanNoLongerExists_PublishesNothing()
        {
            // Arrange
            var planId = Guid.NewGuid();
            _plansMock.Setup(r => r.GetByIdAsync(planId, true)).ReturnsAsync((MealPlan?)null);

            // Act
            await _handler.Handle(
                new OutboxMessage<RecipeAssignedToMealTime>(new RecipeAssignedToMealTime(planId, 1, Guid.NewGuid(), Guid.NewGuid(), 1)),
                CancellationToken.None);

            // Assert
            Assert.Empty(_published);
        }
    }
}
