using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAdminReviewLockedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminReviewLockedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReviewAuthority",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReviewLockedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LastReviewAuthority",
                table: "Documents");
        }
    }
}
