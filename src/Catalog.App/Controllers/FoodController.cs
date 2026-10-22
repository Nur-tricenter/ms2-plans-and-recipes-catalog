using Catalog.Contracts;
using Microsoft.AspNetCore.Authorization;
using Nur.Tricenter.Access.Catalog;
using Catalog.Application.Dto;
using Catalog.Application.Utils;
using Catalog.Application.UseCase.Command.Food.CreateFood;
using Catalog.Application.UseCase.Command.Food.UpdateFood;
using Catalog.Application.UseCase.Query.Food;
using Catalog.App.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.App.Controllers
{
    [ApiController]
    [Route("api/v1/foods")]
    public class FoodController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FoodController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = CatalogPermission.FoodCreate)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFoodCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse<Guid>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse<Guid>.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermission.FoodRead)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetFoodByIdQuery(id));
            if (result.IsSuccess)
                return Ok(ApiResponse<FoodDto>.Ok(result.Value, result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermission.FoodRead)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = new ListFoodsQuery { Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
                return Ok(ApiResponse<PagedList<FoodDto>>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermission.FoodRead)]
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = new SearchFoodsByCategoryQuery(category) { Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
                return Ok(ApiResponse<PagedList<FoodDto>>.Ok(result.Value, result.Message));
            return BadRequest(ApiResponse.Fail(result.Message, result.Errors));
        }

        [Authorize(Policy = CatalogPermission.FoodEdit)]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateFoodCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok(result.Message));
            return NotFound(ApiResponse.Fail(result.Message, result.Errors));
        }

    }
}