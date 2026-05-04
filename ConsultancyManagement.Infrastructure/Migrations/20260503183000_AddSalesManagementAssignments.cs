using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesManagementAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesManagementAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesRecruiterId = table.Column<int>(type: "integer", nullable: false),
                    ManagementUserId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesManagementAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesManagementAssignments_ManagementUsers_ManagementUserId",
                        column: x => x.ManagementUserId,
                        principalTable: "ManagementUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesManagementAssignments_SalesRecruiters_SalesRecruiterId",
                        column: x => x.SalesRecruiterId,
                        principalTable: "SalesRecruiters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagementAssignments_ManagementUserId",
                table: "SalesManagementAssignments",
                column: "ManagementUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesManagementAssignments_SalesRecruiterId",
                table: "SalesManagementAssignments",
                column: "SalesRecruiterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SalesManagementAssignments");
        }
    }
}
