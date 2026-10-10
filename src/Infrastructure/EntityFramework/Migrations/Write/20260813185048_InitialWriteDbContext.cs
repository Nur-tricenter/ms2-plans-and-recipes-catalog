using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.EntityFramework.Migrations.Write
{
    /// <inheritdoc />
    public partial class InitialWriteDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alimento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unidadMedida = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    calorias = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    proteinas = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    carbohidratos = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    grasas = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PlanAlimentario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duracion = table.Column<string>(type: "text", nullable: false),
                    comidasPorDia = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanAlimentario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    instrucciones = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DiaDelPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    numeroDia = table.Column<int>(type: "integer", nullable: false),
                    PlanAlimentarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaDelPlan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaDelPlan_PlanAlimentario_PlanAlimentarioId",
                        column: x => x.PlanAlimentarioId,
                        principalTable: "PlanAlimentario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "IngredienteReceta",
                columns: table => new
                {
                    RecetaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    porcionCantidad = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredienteReceta", x => new { x.RecetaId, x.Id });
                    table.ForeignKey(
                        name: "FK_IngredienteReceta_Receta_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TiempoDeComida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    DiaDelPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiempoDeComida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiempoDeComida_DiaDelPlan_DiaDelPlanId",
                        column: x => x.DiaDelPlanId,
                        principalTable: "DiaDelPlan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AsignacionReceta",
                columns: table => new
                {
                    TiempoDeComidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    recetaId = table.Column<Guid>(type: "uuid", nullable: false),
                    racionCantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionReceta", x => new { x.TiempoDeComidaId, x.Id });
                    table.ForeignKey(
                        name: "FK_AsignacionReceta_TiempoDeComida_TiempoDeComidaId",
                        column: x => x.TiempoDeComidaId,
                        principalTable: "TiempoDeComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiaDelPlan_PlanAlimentarioId",
                table: "DiaDelPlan",
                column: "PlanAlimentarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TiempoDeComida_DiaDelPlanId",
                table: "TiempoDeComida",
                column: "DiaDelPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alimento");

            migrationBuilder.DropTable(
                name: "AsignacionReceta");

            migrationBuilder.DropTable(
                name: "IngredienteReceta");

            migrationBuilder.DropTable(
                name: "TiempoDeComida");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "DiaDelPlan");

            migrationBuilder.DropTable(
                name: "PlanAlimentario");
        }
    }
}
