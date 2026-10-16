using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Factory.Food
{
    using Food = Catalog.Domain.Model.Foods.Food;

    public interface IFoodFactory
    {
        Food Create(string name, string category, UnitOfMeasure unitOfMeasure, NutritionalInfo nutritionalInfo);
    }
}