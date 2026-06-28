using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRoadmap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStudentSkillLevelToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSkillLevel",
                table: "Students");

            migrationBuilder.AddColumn<string>(
                name: "SkillLevel",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillLevel",
                table: "Students");

            migrationBuilder.AddColumn<int>(
                name: "CurrentSkillLevel",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
