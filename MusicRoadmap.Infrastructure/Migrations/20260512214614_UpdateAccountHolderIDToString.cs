using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRoadmap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountHolderIDToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
    // 1. Drop the Foreign Key constraint from the dependent table (Students)
    migrationBuilder.DropForeignKey(
        name: "FK_Students_AccountHolders_AccountHolderId",
        table: "Students");

    // 2. Drop the Index from the dependent table
    migrationBuilder.DropIndex(
        name: "IX_Students_AccountHolderId",
        table: "Students");

    // 3. DROP THE PRIMARY KEY CONSTRAINT from AccountHolders to release the Id column lock
    migrationBuilder.DropUniqueConstraint(
        name: "PK_AccountHolders",
        table: "AccountHolders");

    // 4. Alter the Primary Key column type to string/nvarchar
    migrationBuilder.AlterColumn<string>(
        name: "Id",
        table: "AccountHolders",
        type: "nvarchar(450)",
        nullable: false,
        oldClrType: typeof(Guid),
        oldType: "uniqueidentifier");

    // 5. Alter the Foreign Key column type to string/nvarchar
    migrationBuilder.AlterColumn<string>(
        name: "AccountHolderId",
        table: "Students",
        type: "nvarchar(450)",
        nullable: true,
        oldClrType: typeof(Guid),
        oldType: "uniqueidentifier",
        oldNullable: true);

    // 6. RE-CREATE THE PRIMARY KEY CONSTRAINT on the new string column
    migrationBuilder.AddUniqueConstraint(
        name: "PK_AccountHolders",
        table: "AccountHolders",
        column: "Id");

    // 7. Re-create the index on the dependent table
    migrationBuilder.CreateIndex(
        name: "IX_Students_AccountHolderId",
        table: "Students",
        column: "AccountHolderId");

    // 8. RE-CREATE THE FOREIGN KEY CONSTRAINT matching the new types
    migrationBuilder.AddForeignKey(
        name: "FK_Students_AccountHolders_AccountHolderId",
        table: "Students",
        column: "AccountHolderId",
        principalTable: "AccountHolders",
        principalColumn: "Id",
        onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
    // Roll back by executing steps in perfect reverse order
    migrationBuilder.DropForeignKey(
        name: "FK_Students_AccountHolders_AccountHolderId",
        table: "Students");

    migrationBuilder.DropIndex(
        name: "IX_Students_AccountHolderId",
        table: "Students");

    migrationBuilder.DropUniqueConstraint(
        name: "PK_AccountHolders",
        table: "AccountHolders");

    migrationBuilder.AlterColumn<Guid>(
        name: "AccountHolderId",
        table: "Students",
        type: "uniqueidentifier",
        nullable: true,
        oldClrType: typeof(string),
        oldType: "nvarchar(450)",
        oldNullable: true);

    migrationBuilder.AlterColumn<Guid>(
        name: "Id",
        table: "AccountHolders",
        type: "uniqueidentifier",
        nullable: false,
        oldClrType: typeof(string),
        oldType: "nvarchar(450)");

    migrationBuilder.AddUniqueConstraint(
        name: "PK_AccountHolders",
        table: "AccountHolders",
        column: "Id");

    migrationBuilder.CreateIndex(
        name: "IX_Students_AccountHolderId",
        table: "Students",
        column: "AccountHolderId");

    migrationBuilder.AddForeignKey(
        name: "FK_Students_AccountHolders_AccountHolderId",
        table: "Students",
        column: "AccountHolderId",
        principalTable: "AccountHolders",
        principalColumn: "Id",
        onDelete: ReferentialAction.Restrict);
        }
    }
}
