using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _10x_cards.Migrations
{
    /// <inheritdoc />
    public partial class AddSm2Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EasinessFactor",
                table: "Flashcards",
                type: "REAL",
                nullable: false,
                defaultValue: 2.5);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Flashcards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "Flashcards",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Repetitions",
                table: "Flashcards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Flashcards SET NextReviewDate = CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EasinessFactor",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "Repetitions",
                table: "Flashcards");
        }
    }
}
