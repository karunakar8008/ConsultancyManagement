using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorCode",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Slug", "Name", "IsActive", "CreatedAt" },
                values: new object[] { "default", "Default organization", true, DateTime.UtcNow });

            migrationBuilder.Sql(
                @"SELECT setval(
                    pg_get_serial_sequence('""Organizations""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""Organizations""), 1));");

            const int defaultOrgId = 1;

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Vendors",
                type: "integer",
                nullable: false,
                defaultValue: defaultOrgId);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: defaultOrgId);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "SalesRecruiters",
                type: "integer",
                nullable: false,
                defaultValue: defaultOrgId);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "ManagementUsers",
                type: "integer",
                nullable: false,
                defaultValue: defaultOrgId);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Consultants",
                type: "integer",
                nullable: false,
                defaultValue: defaultOrgId);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_OrganizationId_VendorCode",
                table: "Vendors",
                columns: new[] { "OrganizationId", "VendorCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_EmployeeId",
                table: "Users",
                columns: new[] { "OrganizationId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_NormalizedEmail",
                table: "Users",
                columns: new[] { "OrganizationId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_NormalizedUserName",
                table: "Users",
                columns: new[] { "OrganizationId", "NormalizedUserName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRecruiters_OrganizationId",
                table: "SalesRecruiters",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementUsers_OrganizationId",
                table: "ManagementUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultants_OrganizationId",
                table: "Consultants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultants_Organizations_OrganizationId",
                table: "Consultants",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementUsers_Organizations_OrganizationId",
                table: "ManagementUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesRecruiters_Organizations_OrganizationId",
                table: "SalesRecruiters",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Organizations_OrganizationId",
                table: "Vendors",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultants_Organizations_OrganizationId",
                table: "Consultants");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementUsers_Organizations_OrganizationId",
                table: "ManagementUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesRecruiters_Organizations_OrganizationId",
                table: "SalesRecruiters");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Organizations_OrganizationId",
                table: "Vendors");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_OrganizationId_VendorCode",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId_NormalizedUserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SalesRecruiters_OrganizationId",
                table: "SalesRecruiters");

            migrationBuilder.DropIndex(
                name: "IX_ManagementUsers_OrganizationId",
                table: "ManagementUsers");

            migrationBuilder.DropIndex(
                name: "IX_Consultants_OrganizationId",
                table: "Consultants");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SalesRecruiters");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ManagementUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Consultants");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorCode",
                table: "Vendors",
                column: "VendorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);
        }
    }
}
