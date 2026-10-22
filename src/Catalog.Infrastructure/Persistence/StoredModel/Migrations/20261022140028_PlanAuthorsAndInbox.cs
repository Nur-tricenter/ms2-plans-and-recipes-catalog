using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.StoredModel.Migrations
{
    /// <inheritdoc />
    public partial class PlanAuthorsAndInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "read",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MessageCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "PlanAuthors",
                schema: "read",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsAdministrator = table.Column<bool>(type: "boolean", nullable: false),
                    PlanCount = table.Column<int>(type: "integer", nullable: false),
                    SeenOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanAuthors", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedOn",
                schema: "read",
                table: "InboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_PlanAuthors_UserName",
                schema: "read",
                table: "PlanAuthors",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "read");

            migrationBuilder.DropTable(
                name: "PlanAuthors",
                schema: "read");
        }
    }
}
