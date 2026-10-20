using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Model.MealPlans
{
    public class MealTime : AuditableEntity
    {
        public string Name { get; private set; } = null!;
        public int Order { get; private set; }
        private readonly ICollection<RecipeAssignment> _assignedRecipes = new List<RecipeAssignment>();

        public IEnumerable<RecipeAssignment> AssignedRecipes => _assignedRecipes;

        internal MealTime(string name, int order)
        {
            Id = Guid.NewGuid();
            Name = name;
            Order = order;
        }

        internal void AssignRecipe(Guid recipeId, Ration ration)
        {
            if (_assignedRecipes.Any(r => r.RecipeId == recipeId))
                throw new BussinessRuleValidationException($"Recipe {recipeId} is already assigned to this meal time");

            var assignment = new RecipeAssignment(recipeId, ration);
            _assignedRecipes.Add(assignment);
        }

        internal void RemoveRecipe(Guid recipeId)
        {
            var assignment = _assignedRecipes.FirstOrDefault(r => r.RecipeId == recipeId);
            if (assignment == null)
                throw new BussinessRuleValidationException($"Recipe {recipeId} is not assigned to this meal time");

            _assignedRecipes.Remove(assignment);
        }
    }
}