using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.StoredModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialStoredModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Food",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unitOfMeasure = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    calories = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    protein = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    carbs = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    fat = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Food", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    durationType = table.Column<string>(type: "text", nullable: false),
                    mealsPerDay = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PlanDay",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dayNumber = table.Column<int>(type: "integer", nullable: false),
                    mealPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanDay", x => x.id);
                    table.ForeignKey(
                        name: "FK_PlanDay_MealPlan_mealPlanId",
                        column: x => x.mealPlanId,
                        principalTable: "MealPlan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    foodId = table.Column<Guid>(type: "uuid", nullable: false),
                    portionQuantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    recipeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredient", x => x.id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredient_Recipe_recipeId",
                        column: x => x.recipeId,
                        principalTable: "Recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealTime",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    planDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTime", x => x.id);
                    table.ForeignKey(
                        name: "FK_MealTime_PlanDay_planDayId",
                        column: x => x.planDayId,
                        principalTable: "PlanDay",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignedRecipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    rationQuantity = table.Column<int>(type: "integer", nullable: false),
                    mealTimeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedRecipe", x => x.id);
                    table.ForeignKey(
                        name: "FK_AssignedRecipe_MealTime_mealTimeId",
                        column: x => x.mealTimeId,
                        principalTable: "MealTime",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignedRecipe_mealTimeId",
                table: "AssignedRecipe",
                column: "mealTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTime_planDayId",
                table: "MealTime",
                column: "planDayId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanDay_mealPlanId",
                table: "PlanDay",
                column: "mealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredient_recipeId",
                table: "RecipeIngredient",
                column: "recipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignedRecipe");

            migrationBuilder.DropTable(
                name: "Food");

            migrationBuilder.DropTable(
                name: "RecipeIngredient");

            migrationBuilder.DropTable(
                name: "MealTime");

            migrationBuilder.DropTable(
                name: "Recipe");

            migrationBuilder.DropTable(
                name: "PlanDay");

            migrationBuilder.DropTable(
                name: "MealPlan");
        }
    }
}
