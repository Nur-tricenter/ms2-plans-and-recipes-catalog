using Catalog.Application.Behaviors;
using Catalog.Domain.Factory.Food;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Factory.Recipe;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application
{
    public static class Extensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<IMealPlanFactory, MealPlanFactory>();
            services.AddTransient<IRecipeFactory, RecipeFactory>();
            services.AddTransient<IFoodFactory, FoodFactory>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Extensions).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}
