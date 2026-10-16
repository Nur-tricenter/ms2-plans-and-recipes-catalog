using Catalog.Domain;
using Catalog.Domain.Factory.Food;
using Catalog.Domain.Factory.MealPlan;
using Catalog.Domain.Factory.Recipe;
using Catalog.Domain.Model.Recipes;
using Catalog.Domain.Repository.Food;
using Catalog.Domain.Repository.MealPlan;
using Catalog.Domain.Repository.Recipe;
using Catalog.Domain.ValueObjects;
using Catalog.Infrastructure.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Core;

namespace Catalog.Infrastructure.Seed
{
    public class DbInitializer : IDbInitializer
    {
        private readonly WriteDbContext _context;
        private readonly IFoodFactory _foodFactory;
        private readonly IRecipeFactory _recipeFactory;
        private readonly IMealPlanFactory _mealPlanFactory;
        private readonly IFoodRepository _foodRepo;
        private readonly IRecipeRepository _recipeRepo;
        private readonly IMealPlanRepository _mealPlanRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            WriteDbContext context,
            IFoodFactory foodFactory,
            IRecipeFactory recipeFactory,
            IMealPlanFactory mealPlanFactory,
            IFoodRepository foodRepo,
            IRecipeRepository recipeRepo,
            IMealPlanRepository mealPlanRepo,
            IUnitOfWork unitOfWork,
            ILogger<DbInitializer> logger)
        {
            _context = context;
            _foodFactory = foodFactory;
            _recipeFactory = recipeFactory;
            _mealPlanFactory = mealPlanFactory;
            _foodRepo = foodRepo;
            _recipeRepo = recipeRepo;
            _mealPlanRepo = mealPlanRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Initialize()
        {
            if (await _context.Foods.AnyAsync())
            {
                _logger.LogInformation("Database already seeded, skipping...");
                return;
            }

            _logger.LogInformation("Seeding database...");

            await SeedFoods();
            await SeedRecipes();
            await SeedMealPlans();

            _logger.LogInformation("Database seeding completed.");
        }

        private async Task SeedFoods()
        {
            var foods = new[]
            {
                _foodFactory.Create("CARNE DE RES", "CARNES", UnitOfMeasure.Gram, new NutritionalInfo(100, 250, 26, 0, 15)),
                _foodFactory.Create("POLLO", "CARNES", UnitOfMeasure.Gram, new NutritionalInfo(100, 165, 31, 0, 3.6m)),
                _foodFactory.Create("PAPA", "TUBERCULOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 77, 2, 17, 0.1m)),
                _foodFactory.Create("ARROZ", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 130, 2.7m, 28, 0.3m)),
                _foodFactory.Create("FIDEO", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 131, 5, 25, 1.1m)),
                _foodFactory.Create("HUEVO", "PROTEINAS", UnitOfMeasure.Unit, new NutritionalInfo(60, 93, 7.8m, 0.6m, 6.6m)),
                _foodFactory.Create("TOMATE", "VERDURAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 18, 0.9m, 3.9m, 0.2m)),
                _foodFactory.Create("CEBOLLA", "VERDURAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 40, 1.1m, 9.3m, 0.1m)),
                _foodFactory.Create("ZANAHORIA", "VERDURAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 41, 0.9m, 10, 0.2m)),
                _foodFactory.Create("ARVEJA", "LEGUMBRES", UnitOfMeasure.Gram, new NutritionalInfo(100, 81, 5.4m, 14, 0.4m)),
                _foodFactory.Create("HABA", "LEGUMBRES", UnitOfMeasure.Gram, new NutritionalInfo(100, 88, 8, 14, 0.6m)),
                _foodFactory.Create("CHOCLO", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 96, 3.4m, 21, 1.2m)),
                _foodFactory.Create("QUESO", "LACTEOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 350, 25, 1.3m, 28)),
                _foodFactory.Create("PAN", "GRANOS", UnitOfMeasure.Unit, new NutritionalInfo(50, 132, 4.5m, 24.5m, 1.6m)),
                _foodFactory.Create("LECHE", "LACTEOS", UnitOfMeasure.Milliliter, new NutritionalInfo(100, 42, 3.4m, 5, 1)),
                _foodFactory.Create("PLATANO", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 89, 1.1m, 23, 0.3m)),
                _foodFactory.Create("MANZANA", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 52, 0.3m, 14, 0.2m)),
                _foodFactory.Create("NARANJA", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 47, 0.9m, 12, 0.1m)),
                _foodFactory.Create("LECHUGA", "VERDURAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 15, 1.4m, 2.9m, 0.2m)),
                _foodFactory.Create("QUINUA", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 120, 4.4m, 21, 1.9m)),
                _foodFactory.Create("MAIZ MORADO", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 96, 3.4m, 21, 1.2m)),
                _foodFactory.Create("AZUCAR", "ENDULZANTES", UnitOfMeasure.Gram, new NutritionalInfo(100, 387, 0, 100, 0)),
                _foodFactory.Create("CANELA", "ESPECIAS", UnitOfMeasure.Gram, new NutritionalInfo(10, 0, 0, 0, 0)),
                _foodFactory.Create("DURAZNO", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 39, 0.9m, 10, 0.3m)),
                _foodFactory.Create("HARINA", "GRANOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 364, 10, 76, 1)),
                _foodFactory.Create("PASAS", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 299, 3.1m, 79, 0.5m)),
                _foodFactory.Create("COCO", "FRUTAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 354, 3.3m, 15, 33)),
                _foodFactory.Create("LOCOTO", "VERDURAS", UnitOfMeasure.Gram, new NutritionalInfo(100, 40, 1.9m, 9, 0.4m)),
                _foodFactory.Create("SALCHICHA", "CARNES", UnitOfMeasure.Gram, new NutritionalInfo(100, 290, 12, 3, 26)),
                _foodFactory.Create("CAMOTE", "TUBERCULOS", UnitOfMeasure.Gram, new NutritionalInfo(100, 86, 1.6m, 20, 0.1m))
            };

            foreach (var food in foods)
            {
                await _foodRepo.CreateAsync(food);
            }

            await _unitOfWork.Commit();
            _logger.LogInformation("Seeded {Count} foods", foods.Length);
        }

        private async Task SeedRecipes()
        {
            var foods = await _context.Foods.ToListAsync();
            var beefId = foods.First(f => f.Name == "CARNE DE RES").Id;
            var chickenId = foods.First(f => f.Name == "POLLO").Id;
            var potatoId = foods.First(f => f.Name == "PAPA").Id;
            var riceId = foods.First(f => f.Name == "ARROZ").Id;
            var noodleId = foods.First(f => f.Name == "FIDEO").Id;
            var eggId = foods.First(f => f.Name == "HUEVO").Id;
            var tomatoId = foods.First(f => f.Name == "TOMATE").Id;
            var onionId = foods.First(f => f.Name == "CEBOLLA").Id;
            var carrotId = foods.First(f => f.Name == "ZANAHORIA").Id;
            var peaId = foods.First(f => f.Name == "ARVEJA").Id;
            var beanId = foods.First(f => f.Name == "HABA").Id;
            var cornId = foods.First(f => f.Name == "CHOCLO").Id;
            var cheeseId = foods.First(f => f.Name == "QUESO").Id;
            var breadId = foods.First(f => f.Name == "PAN").Id;
            var milkId = foods.First(f => f.Name == "LECHE").Id;
            var bananaId = foods.First(f => f.Name == "PLATANO").Id;
            var appleId = foods.First(f => f.Name == "MANZANA").Id;
            var orangeId = foods.First(f => f.Name == "NARANJA").Id;
            var lettuceId = foods.First(f => f.Name == "LECHUGA").Id;
            var quinoaId = foods.First(f => f.Name == "QUINUA").Id;
            var purpleCornId = foods.First(f => f.Name == "MAIZ MORADO").Id;
            var sugarId = foods.First(f => f.Name == "AZUCAR").Id;
            var cinnamonId = foods.First(f => f.Name == "CANELA").Id;
            var peachId = foods.First(f => f.Name == "DURAZNO").Id;
            var flourId = foods.First(f => f.Name == "HARINA").Id;
            var raisinsId = foods.First(f => f.Name == "PASAS").Id;
            var coconutId = foods.First(f => f.Name == "COCO").Id;
            var locotoId = foods.First(f => f.Name == "LOCOTO").Id;
            var sausageId = foods.First(f => f.Name == "SALCHICHA").Id;
            var sweetPotatoId = foods.First(f => f.Name == "CAMOTE").Id;

            var recipes = new List<Recipe>();

            // DESAYUNOS
            var r1 = _recipeFactory.Create("Api con Pastel", "Hervir maiz morado con canela y azucar hasta espesar. Servir caliente acompanado de pastel de queso.");
            r1.AddIngredient(purpleCornId, new Portion(100));
            r1.AddIngredient(cinnamonId, new Portion(5));
            r1.AddIngredient(sugarId, new Portion(20));
            recipes.Add(r1);

            var r2 = _recipeFactory.Create("Sandwich de Chola", "Armar sandwich con pan de bola, queso, tomate en rodajas y cebolla. Llevar a la plancha hasta que el queso se derrita.");
            r2.AddIngredient(breadId, new Portion(100));
            r2.AddIngredient(cheeseId, new Portion(80));
            r2.AddIngredient(tomatoId, new Portion(50));
            r2.AddIngredient(onionId, new Portion(30));
            recipes.Add(r2);

            var r3 = _recipeFactory.Create("Mocochinchi", "Hervir durazno deshidratado con canela y azucar. Dejar reposar y servir frio con el durazno rehidratado.");
            r3.AddIngredient(peachId, new Portion(150));
            r3.AddIngredient(cinnamonId, new Portion(5));
            r3.AddIngredient(sugarId, new Portion(20));
            recipes.Add(r3);

            var r4 = _recipeFactory.Create("Cafe con Leche y Pan", "Preparar cafe y mezclar con leche caliente. Servir con pan de bola.");
            r4.AddIngredient(milkId, new Portion(200));
            r4.AddIngredient(breadId, new Portion(100));
            recipes.Add(r4);

            var r5 = _recipeFactory.Create("Batido de Platano con Leche", "Licuar platano maduro con leche y azucar. Servir frio.");
            r5.AddIngredient(bananaId, new Portion(200));
            r5.AddIngredient(milkId, new Portion(200));
            r5.AddIngredient(sugarId, new Portion(15));
            recipes.Add(r5);

            var r6 = _recipeFactory.Create("Bollo de Quinua", "Cocinar quinua y mezclar con queso rallado. Formar bollos y hornear hasta dorar.");
            r6.AddIngredient(quinoaId, new Portion(100));
            r6.AddIngredient(cheeseId, new Portion(60));
            recipes.Add(r6);

            // ALMUERZOS (principales)
            var r7 = _recipeFactory.Create("Silpancho", "Aplanar la carne de res, empanizar con harina y huevo. Freir y servir sobre una cama de arroz, papa cocida y huevo frito. Acompanar con tomate y cebolla picados.");
            r7.AddIngredient(beefId, new Portion(150));
            r7.AddIngredient(riceId, new Portion(100));
            r7.AddIngredient(potatoId, new Portion(100));
            r7.AddIngredient(eggId, new Portion(60));
            r7.AddIngredient(flourId, new Portion(30));
            r7.AddIngredient(tomatoId, new Portion(50));
            r7.AddIngredient(onionId, new Portion(30));
            recipes.Add(r7);

            var r8 = _recipeFactory.Create("Chairo", "Cocinar carne de res con papa, zanahoria, habas y arvejas. Sazonar con cebolla y locoto. Servir caliente.");
            r8.AddIngredient(beefId, new Portion(100));
            r8.AddIngredient(potatoId, new Portion(100));
            r8.AddIngredient(carrotId, new Portion(50));
            r8.AddIngredient(beanId, new Portion(50));
            r8.AddIngredient(peaId, new Portion(50));
            r8.AddIngredient(onionId, new Portion(30));
            r8.AddIngredient(locotoId, new Portion(10));
            recipes.Add(r8);

            var r9 = _recipeFactory.Create("Pique Macho", "Cortar carne de res y salchichas en trozos. Freir con cebolla, tomate y locoto. Servir sobre papas fritas.");
            r9.AddIngredient(beefId, new Portion(150));
            r9.AddIngredient(sausageId, new Portion(100));
            r9.AddIngredient(potatoId, new Portion(150));
            r9.AddIngredient(onionId, new Portion(50));
            r9.AddIngredient(tomatoId, new Portion(50));
            r9.AddIngredient(locotoId, new Portion(10));
            recipes.Add(r9);

            var r10 = _recipeFactory.Create("Arroz con Pollo", "Sofreir pollo con cebolla y tomate. Agregar arroz y cocinar con caldo hasta que este listo.");
            r10.AddIngredient(chickenId, new Portion(150));
            r10.AddIngredient(riceId, new Portion(150));
            r10.AddIngredient(onionId, new Portion(30));
            r10.AddIngredient(tomatoId, new Portion(50));
            recipes.Add(r10);

            var r11 = _recipeFactory.Create("Majadito", "Cocinar arroz con carne de res desmenuzada, cebolla y tomate. Servir con platano frito y huevo.");
            r11.AddIngredient(beefId, new Portion(100));
            r11.AddIngredient(riceId, new Portion(150));
            r11.AddIngredient(onionId, new Portion(30));
            r11.AddIngredient(tomatoId, new Portion(50));
            r11.AddIngredient(bananaId, new Portion(100));
            r11.AddIngredient(eggId, new Portion(60));
            recipes.Add(r11);

            var r12 = _recipeFactory.Create("Fideos con Aji de Carne", "Cocinar fideos. Preparar salsa de aji con carne molida, cebolla y tomate. Mezclar y servir.");
            r12.AddIngredient(noodleId, new Portion(150));
            r12.AddIngredient(beefId, new Portion(100));
            r12.AddIngredient(onionId, new Portion(30));
            r12.AddIngredient(tomatoId, new Portion(50));
            r12.AddIngredient(locotoId, new Portion(10));
            recipes.Add(r12);

            var r13 = _recipeFactory.Create("Churrasco", "Asar la carne de res a la parrilla. Servir con arroz, papa cocida y ensalada de tomate y cebolla.");
            r13.AddIngredient(beefId, new Portion(200));
            r13.AddIngredient(riceId, new Portion(100));
            r13.AddIngredient(potatoId, new Portion(100));
            r13.AddIngredient(tomatoId, new Portion(50));
            r13.AddIngredient(onionId, new Portion(30));
            recipes.Add(r13);

            // POSTRES
            var r14 = _recipeFactory.Create("Arroz con Leche", "Cocinar arroz con leche, azucar y canela hasta que espese. Servir frio o caliente.");
            r14.AddIngredient(riceId, new Portion(80));
            r14.AddIngredient(milkId, new Portion(300));
            r14.AddIngredient(sugarId, new Portion(30));
            r14.AddIngredient(cinnamonId, new Portion(5));
            recipes.Add(r14);

            var r15 = _recipeFactory.Create("Helado de Canela", "Mezclar leche, azucar y canela. Llevar a la heladora o congelar batiendo cada hora.");
            r15.AddIngredient(milkId, new Portion(300));
            r15.AddIngredient(sugarId, new Portion(40));
            r15.AddIngredient(cinnamonId, new Portion(10));
            recipes.Add(r15);

            var r16 = _recipeFactory.Create("Mazamorra", "Cocinar maiz morado con azucar, canela y pasas hasta espesar. Servir con canela espolvoreada.");
            r16.AddIngredient(purpleCornId, new Portion(80));
            r16.AddIngredient(sugarId, new Portion(30));
            r16.AddIngredient(cinnamonId, new Portion(5));
            r16.AddIngredient(raisinsId, new Portion(20));
            recipes.Add(r16);

            var r17 = _recipeFactory.Create("Leche Asada", "Mezclar leche, huevo, azucar y canela. Hornear a bano maria hasta que cuaje. Servir frio.");
            r17.AddIngredient(milkId, new Portion(300));
            r17.AddIngredient(eggId, new Portion(60));
            r17.AddIngredient(sugarId, new Portion(30));
            r17.AddIngredient(cinnamonId, new Portion(5));
            recipes.Add(r17);

            var r18 = _recipeFactory.Create("Bunuelos", "Mezclar harina con huevo, anis y azucar. Freir en aceite caliente hasta dorar. Espolvorear con azucar.");
            r18.AddIngredient(flourId, new Portion(150));
            r18.AddIngredient(eggId, new Portion(30));
            r18.AddIngredient(sugarId, new Portion(20));
            recipes.Add(r18);

            var r19 = _recipeFactory.Create("Cocadas", "Mezclar coco rallado con leche condensada y azucar. Formar bolitas y hornear hasta dorar.");
            r19.AddIngredient(coconutId, new Portion(100));
            r19.AddIngredient(milkId, new Portion(100));
            r19.AddIngredient(sugarId, new Portion(30));
            recipes.Add(r19);

            // CENAS
            var r20 = _recipeFactory.Create("Sopa de Quinua", "Cocinar quinua con zanahoria, arveja, haba y cebolla. Sazonar con sal y servir caliente.");
            r20.AddIngredient(quinoaId, new Portion(80));
            r20.AddIngredient(carrotId, new Portion(50));
            r20.AddIngredient(peaId, new Portion(50));
            r20.AddIngredient(beanId, new Portion(50));
            r20.AddIngredient(onionId, new Portion(30));
            recipes.Add(r20);

            var r21 = _recipeFactory.Create("Tortilla de Papa con Queso", "Rallar papa y mezclar con queso desmenuzado y huevo. Freir en sarten hasta dorar ambos lados.");
            r21.AddIngredient(potatoId, new Portion(150));
            r21.AddIngredient(cheeseId, new Portion(50));
            r21.AddIngredient(eggId, new Portion(30));
            recipes.Add(r21);

            var r22 = _recipeFactory.Create("Ensalada Pacena", "Picar tomate, cebolla y locoto. Mezclar con queso desmenuzado y habas cocidas. Alinar con aceite.");
            r22.AddIngredient(tomatoId, new Portion(100));
            r22.AddIngredient(onionId, new Portion(50));
            r22.AddIngredient(locotoId, new Portion(10));
            r22.AddIngredient(cheeseId, new Portion(50));
            r22.AddIngredient(beanId, new Portion(50));
            recipes.Add(r22);

            var r23 = _recipeFactory.Create("Caldo de Pollo", "Cocinar pollo con papa, zanahoria y arvejas. Sazonar con cebolla. Servir caliente.");
            r23.AddIngredient(chickenId, new Portion(100));
            r23.AddIngredient(potatoId, new Portion(100));
            r23.AddIngredient(carrotId, new Portion(50));
            r23.AddIngredient(peaId, new Portion(50));
            r23.AddIngredient(onionId, new Portion(30));
            recipes.Add(r23);

            var r24 = _recipeFactory.Create("Ensalada de Frutas", "Picar platano, manzana y naranja en trozos. Mezclar y servir fresco.");
            r24.AddIngredient(bananaId, new Portion(100));
            r24.AddIngredient(appleId, new Portion(100));
            r24.AddIngredient(orangeId, new Portion(100));
            recipes.Add(r24);

            var r25 = _recipeFactory.Create("Papa Rellena de Queso", "Hacer pure de papa, rellenar con queso, empanizar y freir. Servir con ensalada de lechuga.");
            r25.AddIngredient(potatoId, new Portion(200));
            r25.AddIngredient(cheeseId, new Portion(60));
            r25.AddIngredient(lettuceId, new Portion(50));
            recipes.Add(r25);

            foreach (var r in recipes)
            {
                await _recipeRepo.CreateAsync(r);
            }

            await _unitOfWork.Commit();
            _logger.LogInformation("Seeded {Count} recipes", recipes.Count);
        }

        private async Task SeedMealPlans()
        {
            var recipes = await _context.Recipes.Include("_ingredients").ToListAsync();

            var breakfasts = new[]
            {
                recipes.First(r => r.Name == "Api con Pastel").Id,
                recipes.First(r => r.Name == "Sandwich de Chola").Id,
                recipes.First(r => r.Name == "Mocochinchi").Id,
                recipes.First(r => r.Name == "Cafe con Leche y Pan").Id,
                recipes.First(r => r.Name == "Batido de Platano con Leche").Id,
                recipes.First(r => r.Name == "Bollo de Quinua").Id
            };

            var lunches = new[]
            {
                recipes.First(r => r.Name == "Silpancho").Id,
                recipes.First(r => r.Name == "Chairo").Id,
                recipes.First(r => r.Name == "Pique Macho").Id,
                recipes.First(r => r.Name == "Arroz con Pollo").Id,
                recipes.First(r => r.Name == "Majadito").Id,
                recipes.First(r => r.Name == "Fideos con Aji de Carne").Id,
                recipes.First(r => r.Name == "Churrasco").Id
            };

            var desserts = new[]
            {
                recipes.First(r => r.Name == "Arroz con Leche").Id,
                recipes.First(r => r.Name == "Helado de Canela").Id,
                recipes.First(r => r.Name == "Mazamorra").Id,
                recipes.First(r => r.Name == "Leche Asada").Id,
                recipes.First(r => r.Name == "Bunuelos").Id,
                recipes.First(r => r.Name == "Cocadas").Id
            };

            var dinners = new[]
            {
                recipes.First(r => r.Name == "Sopa de Quinua").Id,
                recipes.First(r => r.Name == "Tortilla de Papa con Queso").Id,
                recipes.First(r => r.Name == "Ensalada Pacena").Id,
                recipes.First(r => r.Name == "Caldo de Pollo").Id,
                recipes.First(r => r.Name == "Ensalada de Frutas").Id,
                recipes.First(r => r.Name == "Papa Rellena de Queso").Id
            };

            await CreateMealPlan("Plan Comidas Bolivianas 15 Dias", DurationType.FIFTEEN_DAYS, 5, breakfasts, lunches, desserts, dinners);
            await CreateMealPlan("Plan Comidas Bolivianas 30 Dias", DurationType.THIRTY_DAYS, 5, breakfasts, lunches, desserts, dinners);

            await _unitOfWork.Commit();
            _logger.LogInformation("Seeded 2 meal plans (15 and 30 days)");
        }

        private async Task CreateMealPlan(string name, DurationType type, int mealsPerDay, Guid[] breakfasts, Guid[] lunches, Guid[] desserts, Guid[] dinners)
        {
            var mealPlan = _mealPlanFactory.Create(name, new PlanDuration(type), mealsPerDay);
            var totalDays = type == DurationType.FIFTEEN_DAYS ? 15 : 30;

            for (int day = 1; day <= totalDays; day++)
            {
                foreach (var mt in Enum.GetValues<MealTimeType>())
                {
                    mealPlan.AddMealTimeToDay(day, mt);
                }

                var planDay = mealPlan.PlanDays.First(d => d.DayNumber == day);

                var breakfast = planDay.MealTimes.First(t => t.Order == (int)MealTimeType.Breakfast);
                var midMorning = planDay.MealTimes.First(t => t.Order == (int)MealTimeType.MidMorning);
                var lunch = planDay.MealTimes.First(t => t.Order == (int)MealTimeType.Lunch);
                var snack = planDay.MealTimes.First(t => t.Order == (int)MealTimeType.Snack);
                var dinner = planDay.MealTimes.First(t => t.Order == (int)MealTimeType.Dinner);

                mealPlan.AssignRecipeToMealTime(day, breakfast.Id, breakfasts[(day - 1) % breakfasts.Length], new Ration(1));
                mealPlan.AssignRecipeToMealTime(day, midMorning.Id, breakfasts[(day - 1) % breakfasts.Length], new Ration(1));
                mealPlan.AssignRecipeToMealTime(day, lunch.Id, lunches[(day - 1) % lunches.Length], new Ration(1));
                mealPlan.AssignRecipeToMealTime(day, lunch.Id, desserts[(day - 1) % desserts.Length], new Ration(1));
                mealPlan.AssignRecipeToMealTime(day, snack.Id, dinners[(day - 1) % dinners.Length], new Ration(1));
                mealPlan.AssignRecipeToMealTime(day, dinner.Id, dinners[(day - 1) % dinners.Length], new Ration(1));
            }

            await _mealPlanRepo.CreateAsync(mealPlan);
        }
    }
}