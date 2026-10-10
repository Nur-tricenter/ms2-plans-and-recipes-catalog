using Catalog.Application.Utils;
using Shared.Core;
using MediatR;

namespace Catalog.Application.UseCase.Command.Recipe.CreateRecipe
{
    public class CreateRecipeCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = null!;
        public string Instructions { get; set; } = null!;
    }
}