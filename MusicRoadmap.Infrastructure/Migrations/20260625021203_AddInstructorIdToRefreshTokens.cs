using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRoadmap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorIdToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "UserRefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_InstructorId",
                table: "UserRefreshTokens",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRefreshTokens_Instructors_InstructorId",
                table: "UserRefreshTokens",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRefreshTokens_Instructors_InstructorId",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_InstructorId",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "UserRefreshTokens");
        }
    }
}
