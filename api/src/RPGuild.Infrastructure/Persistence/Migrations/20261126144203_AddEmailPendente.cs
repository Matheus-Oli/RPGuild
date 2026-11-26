using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RPGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailPendente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email_pendente",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_pendente",
                table: "users");
        }
    }
}
