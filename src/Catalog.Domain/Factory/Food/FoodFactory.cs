using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.Food
{
    using Food = Catalog.Domain.Model.Foods.Food;

    public class FoodFactory : IFoodFactory
    {
        public Food Create(string name, string category, UnitOfMeasure unitOfMeasure, NutritionalInfo nutritionalInfo)
        {
            return new Food(name, category, unitOfMeasure, nutritionalInfo);
        }
    }
}