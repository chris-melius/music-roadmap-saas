using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRoadmap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToAccountHolderEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AccountHolders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_AccountHolders_Email_InstructorId",
                table: "AccountHolders",
                columns: new[] { "Email", "InstructorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountHolders_Email_InstructorId",
                table: "AccountHolders");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AccountHolders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
