using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan;
using Catalog.Application.UseCase.Command.MealPlan.AddMealTime;
using Catalog.Application.UseCase.Command.MealPlan.AssignRecipeToMealTime;
using Catalog.Application.UseCase.Command.MealPlan.RemoveRecipeFromMealTime;
using Catalog.Application.UseCase.Query.MealPlan;
using Catalog.App.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.App.Controllers
{
    [ApiController]
    [Route("api/v1/meal-plans")]
    public class MealPlanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MealPlanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMealPlanCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse<Guid>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse<Guid>.Fail(result.Message, result.Errors));
        }

        [HttpPost("meal-times")]
        public async Task<IActionResult> AddMealTime([FromBody] AddMealTimeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpPost("assign-recipes")]
        public async Task<IActionResult> AssignRecipe([FromBody] AssignRecipeToMealTimeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpDelete("{mealPlanId:guid}/meal-times/{mealTimeId:guid}/recipes/{recipeId:guid}")]
        public async Task<IActionResult> RemoveRecipeFromMealTime(Guid mealPlanId, Guid mealTimeId, Guid recipeId)
        {
            var result = await _mediator.Send(new RemoveRecipeFromMealTimeCommand
            {
                MealPlanId = mealPlanId,
                MealTimeId = mealTimeId,
                RecipeId = recipeId
            });
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpGet("{mealPlanId:guid}")]
        public async Task<IActionResult> GetComposition(Guid mealPlanId)
        {
            var result = await _mediator.Send(new GetMealPlanCompositionQuery(mealPlanId));
            if (result.IsSuccess)
                return Ok(ApiResponse<MealPlanCompositionDto>.Ok(result.Value, result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var result = await _mediator.Send(new ListMealPlansQuery());
            if (result.IsSuccess)
                return Ok(ApiResponse<IEnumerable<MealPlanDto>>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }
    }
}