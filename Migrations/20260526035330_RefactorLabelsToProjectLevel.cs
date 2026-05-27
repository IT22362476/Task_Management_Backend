using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Task_Manager_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RefactorLabelsToProjectLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskLabels",
                table: "TaskLabels");

            migrationBuilder.DropIndex(
                name: "IX_TaskLabels_TaskId",
                table: "TaskLabels");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "TaskLabels");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TaskLabels");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TaskLabels",
                newName: "LabelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskLabels",
                table: "TaskLabels",
                columns: new[] { "TaskId", "LabelId" });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labels_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskLabels_LabelId",
                table: "TaskLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_ProjectId",
                table: "Labels",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskLabels_Labels_LabelId",
                table: "TaskLabels",
                column: "LabelId",
                principalTable: "Labels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskLabels_Labels_LabelId",
                table: "TaskLabels");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskLabels",
                table: "TaskLabels");

            migrationBuilder.DropIndex(
                name: "IX_TaskLabels_LabelId",
                table: "TaskLabels");

            migrationBuilder.RenameColumn(
                name: "LabelId",
                table: "TaskLabels",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "TaskLabels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TaskLabels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskLabels",
                table: "TaskLabels",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLabels_TaskId",
                table: "TaskLabels",
                column: "TaskId");
        }
    }
}
