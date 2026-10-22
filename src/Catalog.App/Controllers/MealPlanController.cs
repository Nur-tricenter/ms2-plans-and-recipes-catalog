using Catalog.Application.Dto;
using System.Security.Claims;
using Catalog.Contracts;
using Microsoft.AspNetCore.Authorization;
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
    /// <remarks>
/// Class-level <c>[Authorize]</c> with no policy means "any authenticated caller", which is the
/// floor every endpoint here needs: a plan's author is read from the <c>sub</c> claim, so an
/// anonymous caller would have nothing to attribute. Individual methods narrow further with their
/// own policy.
/// </remarks>
[Authorize]
[Route("api/v1/meal-plans")]
    public class MealPlanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MealPlanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = CatalogPermissions.MealPlanCreate)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMealPlanCommand command)
        {
            // The plan's author comes from the validated bearer token, never from the body. The
            // gateway has already verified this token against the identity service, so the subject
            // claim is proof of who the caller is; trusting a body field would let any
            // authenticated caller create plans under somebody else's account.
            var subject = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            command.AuthorIdFromToken = Guid.TryParse(subject, out var authorId)
                ? authorId
                : Guid.Empty;

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse<Guid>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse<Guid>.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermissions.MealPlanEdit)]
        [HttpPost("meal-times")]
        public async Task<IActionResult> AddMealTime([FromBody] AddMealTimeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermissions.MealPlanEdit)]
        [HttpPost("assign-recipes")]
        public async Task<IActionResult> AssignRecipe([FromBody] AssignRecipeToMealTimeCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermissions.MealPlanEdit)]
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

        [Authorize(Policy = CatalogPermissions.MealPlanRead)]
        [HttpGet("{mealPlanId:guid}")]
        public async Task<IActionResult> GetComposition(Guid mealPlanId)
        {
            var result = await _mediator.Send(new GetMealPlanCompositionQuery(mealPlanId));
            if (result.IsSuccess)
                return Ok(ApiResponse<MealPlanCompositionDto>.Ok(result.Value, result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermissions.MealPlanRead)]
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