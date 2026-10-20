using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Domain.Factory.Recipe;
using Catalog.Domain.Repository.Recipe;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.UseCase.Command.Recipe.CreateRecipe
{
    public class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, Result<Guid>>
    {
        private readonly IRecipeRepository _repository;
        private readonly IRecipeFactory _factory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateRecipeHandler> _logger;

        public CreateRecipeHandler(
            IRecipeRepository repository,
            IRecipeFactory factory,
            IUnitOfWork unitOfWork,
            ILogger<CreateRecipeHandler> logger)
        {
            _repository = repository;
            _factory = factory;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateRecipeCommand request, CancellationToken cancellationToken = default)
        {
            try
            {
                var recipe = _factory.Create(request.Name, request.Instructions);

                await _repository.AddAsync(recipe);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<Guid>.Ok(recipe.Id, "Recipe created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe");
                return Result.Fail<Guid>(ex.Message);
            }
        }
    }
}