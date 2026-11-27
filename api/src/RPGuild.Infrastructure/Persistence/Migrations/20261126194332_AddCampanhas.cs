using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RPGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampanhas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_systems",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    capa_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    banner_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaigns_game_systems_system_id",
                        column: x => x.system_id,
                        principalTable: "game_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_campaigns_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_members",
                columns: table => new
                {
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_members", x => new { x.campaign_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_campaign_members_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaign_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_user_order",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posicao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_user_order", x => new { x.user_id, x.campaign_id });
                    table.ForeignKey(
                        name: "fk_campaign_user_order_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaign_user_order_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "game_systems",
                columns: new[] { "id", "ativo", "nome", "slug" },
                values: new object[,]
                {
                    { 1, true, "D&D 5e (regras de 2024)", "dnd2024" },
                    { 2, false, "Ordem Paranormal 2", "ordem2" },
                    { 3, false, "Tormenta 20", "t20" },
                    { 4, false, "Call of Cthulhu", "coc" },
                    { 5, false, "Deadlands", "deadlands" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_members_user_id",
                table: "campaign_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_user_order_campaign_id",
                table: "campaign_user_order",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_owner_id",
                table: "campaigns",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_system_id",
                table: "campaigns",
                column: "system_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_systems_slug",
                table: "game_systems",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_members");

            migrationBuilder.DropTable(
                name: "campaign_user_order");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "game_systems");
        }
    }
}
