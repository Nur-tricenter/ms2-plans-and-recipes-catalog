using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Command.Recipe.CreateRecipe;
using Catalog.Application.UseCase.Command.Recipe.AddIngredient;
using Catalog.Application.UseCase.Command.Recipe.RemoveIngredient;
using Catalog.Application.UseCase.Query.Recipe;
using Catalog.WebApi.Utils;
using Shared.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/recipes")]
    public class RecipeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RecipeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse<Guid>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse<Guid>.Fail(result.Message, result.Errors));
        }

        [HttpPost("ingredients")]
        public async Task<IActionResult> AddIngredient([FromBody] AddIngredientCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpDelete("{recipeId:guid}/ingredients/{foodId:guid}")]
        public async Task<IActionResult> RemoveIngredient(Guid recipeId, Guid foodId)
        {
            var result = await _mediator.Send(new RemoveIngredientCommand { RecipeId = recipeId, FoodId = foodId });
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetRecipeByIdQuery(id));
            if (result.IsSuccess)
                return Ok(ApiResponse<RecipeDto>.Ok(result.Value, result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = new ListRecipesQuery { Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
                return Ok(ApiResponse<PagedList<RecipeDto>>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpGet("{recipeId:guid}/nutritional-info")]
        public async Task<IActionResult> GetNutritionalInfo(Guid recipeId)
        {
            var result = await _mediator.Send(new GetRecipeNutritionalInfoQuery(recipeId));
            if (result.IsSuccess)
                return Ok(ApiResponse<NutritionalInfoDto>.Ok(result.Value, result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }
    }
}