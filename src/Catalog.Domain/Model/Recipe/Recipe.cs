using Catalog.Domain.Model.Foods;
using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;
using Catalog.Domain.ValueObjects;
using Catalog.Domain.Event.Recipe;

namespace Catalog.Domain.Model.Recipes
{
    public class Recipe : AuditableAggregateRoot
    {
        public RecipeName Name { get; private set; }
        public string Instructions { get; private set; }
        private readonly ICollection<RecipeIngredient> _ingredients;

        public IEnumerable<RecipeIngredient> Ingredients => _ingredients;

        private Recipe() { Name = null!; Instructions = null!; _ingredients = null!; }

        public Recipe(RecipeName name, string instructions)
        {
            if (string.IsNullOrWhiteSpace(instructions))
                throw new BussinessRuleValidationException("Recipe instructions cannot be empty");

            Id = Guid.NewGuid();
            Name = name;
            Instructions = instructions;
            _ingredients = new List<RecipeIngredient>();

            AddDomainEvent(new RecipeCreated(Id, Name));
        }

        public void Update(string instructions)
        {
            if (string.IsNullOrWhiteSpace(instructions))
                throw new BussinessRuleValidationException("Recipe instructions cannot be empty");

            Instructions = instructions;

            AddDomainEvent(new RecipeUpdated(Id, Name, Instructions));
        }

        public void AddIngredient(Guid foodId, Portion portion)
        {
            if (_ingredients.Any(i => i.FoodId == foodId))
                throw new BussinessRuleValidationException($"Food {foodId} is already added to the recipe");

            var ingredient = new RecipeIngredient(foodId, portion);
            _ingredients.Add(ingredient);

            AddDomainEvent(new IngredientAddedToRecipe(Id, foodId, portion.Quantity));
        }

        public void RemoveIngredient(Guid foodId)
        {
            var ingredient = _ingredients.FirstOrDefault(i => i.FoodId == foodId);
            if (ingredient == null)
                throw new BussinessRuleValidationException($"Ingredient {foodId} does not exist in the recipe");

            _ingredients.Remove(ingredient);

            AddDomainEvent(new IngredientRemovedFromRecipe(Id, foodId, ingredient.Portion.Quantity));
        }

        public NutritionalInfo CalculateTotalNutritionalInfo(Func<Guid, Food> getFood)
        {
            decimal totalCalories = 0, totalProtein = 0, totalCarbs = 0, totalFat = 0;

            foreach (var ing in _ingredients)
            {
                var food = getFood(ing.FoodId);
                if (food == null) continue;

                var factor = ing.Portion.Quantity / food.BaseNutritionalInfo.Quantity;
                totalCalories += food.BaseNutritionalInfo.Calories * factor;
                totalProtein += food.BaseNutritionalInfo.Protein * factor;
                totalCarbs += food.BaseNutritionalInfo.Carbs * factor;
                totalFat += food.BaseNutritionalInfo.Fat * factor;
            }

            return new NutritionalInfo(1, totalCalories, totalProtein, totalCarbs, totalFat);
        }
    }
}