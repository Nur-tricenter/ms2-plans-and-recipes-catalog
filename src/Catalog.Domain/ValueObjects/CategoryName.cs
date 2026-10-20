using Joseco.DDD.Core.Abstractions;
using Catalog.Domain.Primitives;

namespace Catalog.Domain.ValueObjects
{
    public record CategoryName : ValueObject
    {
        public string Name { get; init; }

        public CategoryName(string name)
        {
            CheckRule(new StringNotNullOrEmptyRule(name));
            if (name.Length > 50)
                throw new BussinessRuleValidationException("Category name cannot be more than 50 characters");
            Name = name;
        }

        public static implicit operator string(CategoryName value) => value.Name;

        public static implicit operator CategoryName(string name) => new(name);
    }
}