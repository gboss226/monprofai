using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonProfAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerProfileAndNotionMastery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<float>(
                name: "Progress",
                table: "UserCourses",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real",
                oldDefaultValue: 0f);

            migrationBuilder.CreateTable(
                name: "LearnerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolLevel = table.Column<int>(type: "integer", nullable: false),
                    LearningObjectives = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExplanationPreferences = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotionMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotionKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MasteryScore = table.Column<double>(type: "double precision", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionMasteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotionMasteries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProfiles_UserId",
                table: "LearnerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotionMasteries_UserId_NotionKey",
                table: "NotionMasteries",
                columns: new[] { "UserId", "NotionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerProfiles");

            migrationBuilder.DropTable(
                name: "NotionMasteries");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<float>(
                name: "Progress",
                table: "UserCourses",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real");
        }
    }
}
