using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DocSequence.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentNumber = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeId);
                    table.CheckConstraint("CK_DocumentTypes_CurrentNumber", "CurrentNumber >= 0");
                    table.CheckConstraint("CK_DocumentTypes_Prefix", "Prefix COLLATE Latin1_General_BIN2 NOT LIKE '%[^A-Z]%' AND LEN(Prefix) BETWEEN 2 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "GeneratedDocuments",
                columns: table => new
                {
                    GeneratedDocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    Identifier = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EngineerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedDocuments", x => x.GeneratedDocumentId);
                    table.ForeignKey(
                        name: "FK_GeneratedDocuments_DocumentTypes",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DocumentTypes",
                columns: new[] { "DocumentTypeId", "CurrentNumber", "IsActive", "Name", "Prefix", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 10428L, true, "CXY Drawing", "CXY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 10428L, true, "PXY Drawing", "PXY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "UX_DocumentTypes_Prefix",
                table: "DocumentTypes",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedDocuments_CreatedAt",
                table: "GeneratedDocuments",
                columns: new[] { "CreatedAt", "GeneratedDocumentId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_GeneratedDocuments_DocumentTypeId_Number",
                table: "GeneratedDocuments",
                columns: new[] { "DocumentTypeId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GeneratedDocuments_Identifier",
                table: "GeneratedDocuments",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GeneratedDocuments_RequestKey",
                table: "GeneratedDocuments",
                column: "RequestKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedDocuments");

            migrationBuilder.DropTable(
                name: "DocumentTypes");
        }
    }
}
