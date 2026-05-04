using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VendorSubmissionInterviewReachOutExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Vendors",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactProofFilePath",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesRecruiterId",
                table: "Vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorCode",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofFilePath",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionCode",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewCode",
                table: "Interviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteProofFilePath",
                table: "Interviews",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """UPDATE "Vendors" SET "VendorCode" = 'VEN-' || lpad("Id"::text, 10, '0') WHERE "VendorCode" IS NULL OR trim(both from coalesce("VendorCode", '')) = '';""");

            migrationBuilder.Sql(
                """UPDATE "Submissions" SET "SubmissionCode" = 'SUB-' || lpad("Id"::text, 10, '0') WHERE "SubmissionCode" IS NULL OR trim(both from coalesce("SubmissionCode", '')) = '';""");

            migrationBuilder.Sql(
                """UPDATE "Interviews" SET "InterviewCode" = 'INT-' || lpad("Id"::text, 10, '0') WHERE "InterviewCode" IS NULL OR trim(both from coalesce("InterviewCode", '')) = '';""");

            migrationBuilder.AlterColumn<string>(
                name: "VendorCode",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubmissionCode",
                table: "Submissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InterviewCode",
                table: "Interviews",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ConsultantVendorReachOuts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsultantId = table.Column<int>(type: "integer", nullable: false),
                    ReachedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VendorName = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantVendorReachOuts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantVendorReachOuts_Consultants_ConsultantId",
                        column: x => x.ConsultantId,
                        principalTable: "Consultants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_SalesRecruiterId",
                table: "Vendors",
                column: "SalesRecruiterId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorCode",
                table: "Vendors",
                column: "VendorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantVendorReachOuts_ConsultantId",
                table: "ConsultantVendorReachOuts",
                column: "ConsultantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_SalesRecruiters_SalesRecruiterId",
                table: "Vendors",
                column: "SalesRecruiterId",
                principalTable: "SalesRecruiters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_SalesRecruiters_SalesRecruiterId",
                table: "Vendors");

            migrationBuilder.DropTable(
                name: "ConsultantVendorReachOuts");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_SalesRecruiterId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ContactProofFilePath",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SalesRecruiterId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VendorCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ProofFilePath",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmissionCode",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "InterviewCode",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "InviteProofFilePath",
                table: "Interviews");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
