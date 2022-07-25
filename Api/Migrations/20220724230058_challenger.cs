using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class challenger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tip",
                table: "ProgrammingChallenges");

            migrationBuilder.RenameColumn(
                name: "Question",
                table: "ProgrammingChallenges",
                newName: "QueryParameter");

            migrationBuilder.RenameColumn(
                name: "Explanation",
                table: "ProgrammingChallenges",
                newName: "Description");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QueryParameter",
                table: "ProgrammingChallenges",
                newName: "Question");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ProgrammingChallenges",
                newName: "Explanation");

            migrationBuilder.AddColumn<string>(
                name: "Tip",
                table: "ProgrammingChallenges",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
