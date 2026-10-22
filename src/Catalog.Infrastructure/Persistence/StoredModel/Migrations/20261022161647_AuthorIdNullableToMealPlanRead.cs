using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.StoredModel.Migrations
{
    /// <inheritdoc />
    public partial class AuthorIdNullableToMealPlanRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanCount",
                schema: "read",
                table: "PlanAuthors");

            migrationBuilder.AddColumn<Guid>(
                name: "authorId",
                schema: "read",
                table: "MealPlan",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "authorId",
                schema: "read",
                table: "MealPlan");

            migrationBuilder.AddColumn<int>(
                name: "PlanCount",
                schema: "read",
                table: "PlanAuthors",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
