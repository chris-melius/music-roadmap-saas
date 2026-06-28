using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRoadmap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotesToStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Students");
        }
    }
}
