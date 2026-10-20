using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;
using Catalog.Domain.ValueObjects;
using Catalog.Domain.Event.Food;

namespace Catalog.Domain.Model.Foods
{
    public class Food : AuditableAggregateRoot
    {
        public FoodName Name { get; private set; }
        public CategoryName Category { get; private set; }
        public UnitOfMeasure UnitOfMeasure { get; private set; }
        public NutritionalInfo BaseNutritionalInfo { get; private set; }

        private Food() { Name = null!; Category = null!; BaseNutritionalInfo = null!; }

        public Food(FoodName name, CategoryName category, UnitOfMeasure unitOfMeasure, NutritionalInfo baseNutritionalInfo)
        {
            Id = Guid.NewGuid();
            Name = name;
            Category = category;
            UnitOfMeasure = unitOfMeasure;
            BaseNutritionalInfo = baseNutritionalInfo;

            AddDomainEvent(new FoodCreated(Id, Name, Category, BaseNutritionalInfo, UnitOfMeasure));
        }

        public void UpdateNutritionalInfo(NutritionalInfo info)
        {
            var previous = BaseNutritionalInfo;
            BaseNutritionalInfo = info;

            AddDomainEvent(new FoodNutritionalInfoUpdated(Id, previous, info));
        }

        public void Update(FoodName name, CategoryName category, UnitOfMeasure unitOfMeasure, NutritionalInfo info)
        {
            Name = name;
            Category = category;
            UnitOfMeasure = unitOfMeasure;
            BaseNutritionalInfo = info;

            AddDomainEvent(new FoodUpdated(Id, name, category, unitOfMeasure, info));
        }
    }
}