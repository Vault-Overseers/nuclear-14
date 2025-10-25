using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class _20251016_Upstream_merge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employer",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "lifepath",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nationality",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "special",
                columns: table => new
                {
                    special_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    special_name = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_special", x => x.special_id);
                    table.ForeignKey(
                        name: "FK_special_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_special_profile_id",
                table: "special",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_special_profile_id_special_name",
                table: "special",
                columns: new[] { "profile_id", "special_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "special");

            migrationBuilder.DropColumn(
                name: "employer",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "lifepath",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "nationality",
                table: "profile");
        }
    }
}
