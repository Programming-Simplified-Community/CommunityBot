using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    public partial class challengetweak : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "TestResults",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt",
                table: "ProgrammingChallengeSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsTimed",
                table: "ProgrammingChallenges",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Attempt",
                table: "ProgrammingChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "IsTimed",
                table: "ProgrammingChallenges");
        }
    }
}
