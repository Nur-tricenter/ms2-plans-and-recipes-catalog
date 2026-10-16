using Shared.Core;
using Shared.Rules;

namespace Catalog.Domain.ValueObjects
{
    public record RecipeName : ValueObject
    {
        public string Name { get; init; }

        public RecipeName(string name)
        {
            CheckRule(new StringNotNullOrEmptyRule(name));
            if (name.Length > 200)
                throw new BussinessRuleValidationException("Recipe name cannot be more than 200 characters");
            Name = name;
        }

        public static implicit operator string(RecipeName value) => value.Name;

        public static implicit operator RecipeName(string name) => new(name);
    }
}
