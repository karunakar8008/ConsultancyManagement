using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorLinkedConsultantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedConsultantId",
                table: "Vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_LinkedConsultantId",
                table: "Vendors",
                column: "LinkedConsultantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_Consultants_LinkedConsultantId",
                table: "Vendors",
                column: "LinkedConsultantId",
                principalTable: "Consultants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_Consultants_LinkedConsultantId",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_LinkedConsultantId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "LinkedConsultantId",
                table: "Vendors");
        }
    }
}
