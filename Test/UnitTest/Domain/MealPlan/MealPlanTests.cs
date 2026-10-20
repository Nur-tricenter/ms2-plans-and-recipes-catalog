using Catalog.Domain;
using Catalog.Domain.Primitives;
using Catalog.Domain.Event.MealPlan;
using Catalog.Domain.Model.MealPlans;
using Catalog.Domain.ValueObjects;
using Joseco.DDD.Core.Abstractions;
using Xunit;

namespace Catalog.Tests.Domain
{
    public class MealPlanTests
    {
        [Theory]
        [InlineData(DurationType.FIFTEEN_DAYS, 15)]
        [InlineData(DurationType.THIRTY_DAYS, 30)]
        public void Constructor_ValidDuration_CreatesPlanWithExpectedDays(DurationType type, int expectedDays)
        {
            // Act
            var plan = new MealPlan(new MealPlanName("Plan Saludable"), new PlanDuration(type), 3);

            // Assert
            Assert.NotEqual(Guid.Empty, plan.Id);
            Assert.Equal("Plan Saludable", plan.Name.Name);
            Assert.Equal(3, plan.MealsPerDay);
            Assert.Equal(expectedDays, plan.PlanDays.Count());
            var evt = Assert.IsType<MealPlanCreated>(Assert.Single(plan.DomainEvents));
            Assert.Equal(plan.Id, evt.PlanId);
            Assert.Equal(expectedDays, evt.TotalDays);
        }

        [Fact]
        public void AddMealTimeToDay_ValidDay_AddsMealTime()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);

            // Assert
            var day = plan.PlanDays.First(d => d.DayNumber == 1);
            var mealTime = Assert.Single(day.MealTimes);
            Assert.Equal("Breakfast", mealTime.Name);
            Assert.Equal(1, mealTime.Order);
        }

        [Fact]
        public void AddMealTimeToDay_InvalidDay_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AddMealTimeToDay(16, MealTimeType.Breakfast));
            Assert.Contains("Day 16 does not exist in the plan", ex.Details);
        }

        [Fact]
        public void AddMealTimeToDay_DuplicateOrder_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AddMealTimeToDay(1, MealTimeType.Breakfast));
            Assert.Contains("Meal time with order 1 already exists on day", ex.Details);
        }

        [Fact]
        public void AssignRecipeToMealTime_WithDayAndExistingTime_AssignsRecipeAndQueuesEvent()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipeId = Guid.NewGuid();

            // Act
            plan.AssignRecipeToMealTime(1, mealTime.Id, recipeId, new Ration(2));

            // Assert
            var assignment = Assert.Single(mealTime.AssignedRecipes);
            Assert.Equal(recipeId, assignment.RecipeId);
            Assert.Equal(2, assignment.Ration.Quantity);
            // 3 evts: MealPlanCreated, MealTimeAddedToPlan, RecipeAssignedToMealTime
            Assert.Equal(3, plan.DomainEvents.Count);
            var evt = Assert.IsType<RecipeAssignedToMealTime>(plan.DomainEvents.Last());
            Assert.Equal(1, evt.DayNumber);
            Assert.Equal(recipeId, evt.RecipeId);
        }

        [Fact]
        public void AssignRecipeToMealTime_InvalidDay_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AssignRecipeToMealTime(16, Guid.NewGuid(), Guid.NewGuid(), new Ration(2)));
            Assert.Contains("Day 16 does not exist in the plan", ex.Details);
        }

        [Fact]
        public void AssignRecipeToMealTime_TimeNotInDay_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AssignRecipeToMealTime(1, Guid.NewGuid(), Guid.NewGuid(), new Ration(2)));
            Assert.Contains("does not exist on day 1", ex.Details);
        }

        [Fact]
        public void AssignRecipeToMealTime_WithoutDay_FindsTimeInAnyDayAndAssigns()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(3, MealTimeType.Lunch);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 3).MealTimes.Single();
            var recipeId = Guid.NewGuid();

            // Act
            plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(1));

            // Assert
            var assignment = Assert.Single(mealTime.AssignedRecipes);
            Assert.Equal(recipeId, assignment.RecipeId);
            var evt = Assert.IsType<RecipeAssignedToMealTime>(plan.DomainEvents.Last());
            Assert.Equal(3, evt.DayNumber);
        }

        [Fact]
        public void AssignRecipeToMealTime_WithoutDayAndTimeNotFound_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AssignRecipeToMealTime(Guid.NewGuid(), Guid.NewGuid(), new Ration(1)));
            Assert.Contains("does not exist in the plan", ex.Details);
        }

        [Fact]
        public void AssignRecipeToMealTime_DuplicateRecipeInTime_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipeId = Guid.NewGuid();
            plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(1));

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(2)));
            Assert.Contains("is already assigned", ex.Details);
        }

        [Fact]
        public void RemoveRecipeFromMealTime_ExistingAssignment_RemovesAndQueuesEvent()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();
            var recipeId = Guid.NewGuid();
            plan.AssignRecipeToMealTime(mealTime.Id, recipeId, new Ration(1));

            // Act
            plan.RemoveRecipeFromMealTime(mealTime.Id, recipeId);

            // Assert
            Assert.Empty(mealTime.AssignedRecipes);
            // 4 evts: MealPlanCreated, MealTimeAddedToPlan, RecipeAssignedToMealTime, RecipeRemovedFromMealTime
            Assert.Equal(4, plan.DomainEvents.Count);
            var evt = Assert.IsType<RecipeRemovedFromMealTime>(plan.DomainEvents.Last());
            Assert.Equal(recipeId, evt.RecipeId);
        }

        [Fact]
        public void RemoveRecipeFromMealTime_TimeNotFound_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.RemoveRecipeFromMealTime(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Contains("does not exist in the plan", ex.Details);
        }

        [Fact]
        public void RemoveRecipeFromMealTime_RecipeNotAssigned_ThrowsException()
        {
            // Arrange
            var plan = new MealPlan(new MealPlanName("Plan"), new PlanDuration(DurationType.FIFTEEN_DAYS), 3);
            plan.AddMealTimeToDay(1, MealTimeType.Breakfast);
            var mealTime = plan.PlanDays.First(d => d.DayNumber == 1).MealTimes.Single();

            // Act + Assert
            var ex = Assert.Throws<BussinessRuleValidationException>(() =>
                plan.RemoveRecipeFromMealTime(mealTime.Id, Guid.NewGuid()));
            Assert.Contains("is not assigned", ex.Details);
        }
    }
}
