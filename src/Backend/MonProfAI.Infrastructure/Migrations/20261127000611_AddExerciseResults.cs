using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonProfAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    HintsUsed = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    MasteryBefore = table.Column<double>(type: "double precision", nullable: false),
                    MasteryAfter = table.Column<double>(type: "double precision", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseResults", x => x.Id);
                    table.CheckConstraint("CK_ExerciseResults_Counts", "\"HintsUsed\" >= 0 AND \"Attempts\" >= 0");
                    table.CheckConstraint("CK_ExerciseResults_Mastery", "\"MasteryBefore\" BETWEEN 0 AND 1 AND \"MasteryAfter\" BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_ExerciseResults_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseResults_ExerciseId",
                table: "ExerciseResults",
                column: "ExerciseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseResults");
        }
    }
}
