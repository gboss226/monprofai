using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonProfAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingFieldsToLearnerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplanationPreferences",
                table: "LearnerProfiles");

            migrationBuilder.AddColumn<int>(
                name: "Goal",
                table: "LearnerProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "LearnerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreferredLearningStyle",
                table: "LearnerProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "StrugglingSubjects",
                table: "LearnerProfiles",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goal",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLearningStyle",
                table: "LearnerProfiles");

            migrationBuilder.DropColumn(
                name: "StrugglingSubjects",
                table: "LearnerProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ExplanationPreferences",
                table: "LearnerProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
