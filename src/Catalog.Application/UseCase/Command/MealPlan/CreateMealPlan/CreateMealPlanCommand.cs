using Catalog.Application.Utils;
using Joseco.DDD.Core.Abstractions;
using MediatR;
using Catalog.Application.Abstractions;

namespace Catalog.Application.UseCase.Command.MealPlan.CreateMealPlan
{
    public class CreateMealPlanCommand : ICommand<Result<Guid>>
    {
        public string Name { get; set; } = null!;
        public string DurationType { get; set; } = null!;
        public int MealsPerDay { get; set; }

        /// <summary>
        /// The account that owns the plan, read from the bearer token by the controller.
        /// </summary>
        /// <remarks>
        /// Named distinctly from any wire property so it cannot be confused with something the
        /// caller sends. The author is an authorization fact proven by the token, never an input:
        /// accepting it from the body would let any authenticated caller name somebody else as the
        /// owner of a plan.
        /// </remarks>
        public Guid AuthorIdFromToken { get; set; }
    }
}