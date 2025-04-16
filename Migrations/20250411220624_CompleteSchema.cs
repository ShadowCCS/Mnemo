using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MnemoProject.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EaseFactor",
                table: "Decks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewDate",
                table: "Decks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "Decks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepetitionCount",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EaseFactor",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "LastReviewDate",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "RepetitionCount",
                table: "Decks");
        }
    }
}
