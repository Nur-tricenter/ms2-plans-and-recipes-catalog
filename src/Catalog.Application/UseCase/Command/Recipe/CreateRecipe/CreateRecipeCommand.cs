using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.Recipe.CreateRecipe
{
    public class CreateRecipeCommand : ICommand<Result<Guid>>
    {
        public string Name { get; set; } = null!;
        public string Instructions { get; set; } = null!;
    }
}