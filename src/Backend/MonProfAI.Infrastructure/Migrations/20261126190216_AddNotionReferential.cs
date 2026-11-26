using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonProfAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotionReferential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mastery rows were keyed by a free-text NotionKey that cannot be mapped to the new
            // Notions table (no application code wrote them yet). Purge them so the NOT NULL
            // foreign key below can be created on existing databases too.
            migrationBuilder.Sql("DELETE FROM \"NotionMasteries\";");

            migrationBuilder.DropIndex(
                name: "IX_NotionMasteries_UserId_NotionKey",
                table: "NotionMasteries");

            migrationBuilder.DropColumn(
                name: "NotionKey",
                table: "NotionMasteries");

            migrationBuilder.AddColumn<Guid>(
                name: "NotionId",
                table: "NotionMasteries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolLevel = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.CheckConstraint("CK_Chapters_Order_Positive", "\"Order\" > 0");
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notions", x => x.Id);
                    table.CheckConstraint("CK_Notions_Order_Positive", "\"Order\" > 0");
                    table.ForeignKey(
                        name: "FK_Notions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotionPrerequisites",
                columns: table => new
                {
                    NotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteNotionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotionPrerequisites", x => new { x.NotionId, x.PrerequisiteNotionId });
                    table.CheckConstraint("CK_NotionPrerequisites_NotSelf", "\"NotionId\" <> \"PrerequisiteNotionId\"");
                    table.ForeignKey(
                        name: "FK_NotionPrerequisites_Notions_NotionId",
                        column: x => x.NotionId,
                        principalTable: "Notions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotionPrerequisites_Notions_PrerequisiteNotionId",
                        column: x => x.PrerequisiteNotionId,
                        principalTable: "Notions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotionMasteries_NotionId",
                table: "NotionMasteries",
                column: "NotionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotionMasteries_UserId_NotionId",
                table: "NotionMasteries",
                columns: new[] { "UserId", "NotionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId_SchoolLevel_Order",
                table: "Chapters",
                columns: new[] { "SubjectId", "SchoolLevel", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotionPrerequisites_PrerequisiteNotionId",
                table: "NotionPrerequisites",
                column: "PrerequisiteNotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notions_ChapterId_Order",
                table: "Notions",
                columns: new[] { "ChapterId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NotionMasteries_Notions_NotionId",
                table: "NotionMasteries",
                column: "NotionId",
                principalTable: "Notions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rows reference notions that are about to be dropped and cannot be turned back into keys.
            migrationBuilder.Sql("DELETE FROM \"NotionMasteries\";");

            migrationBuilder.DropForeignKey(
                name: "FK_NotionMasteries_Notions_NotionId",
                table: "NotionMasteries");

            migrationBuilder.DropTable(
                name: "NotionPrerequisites");

            migrationBuilder.DropTable(
                name: "Notions");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_NotionMasteries_NotionId",
                table: "NotionMasteries");

            migrationBuilder.DropIndex(
                name: "IX_NotionMasteries_UserId_NotionId",
                table: "NotionMasteries");

            migrationBuilder.DropColumn(
                name: "NotionId",
                table: "NotionMasteries");

            migrationBuilder.AddColumn<string>(
                name: "NotionKey",
                table: "NotionMasteries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NotionMasteries_UserId_NotionKey",
                table: "NotionMasteries",
                columns: new[] { "UserId", "NotionKey" },
                unique: true);
        }
    }
}
