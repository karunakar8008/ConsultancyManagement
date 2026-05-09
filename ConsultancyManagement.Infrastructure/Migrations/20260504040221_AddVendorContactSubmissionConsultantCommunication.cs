using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsultancyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorContactSubmissionConsultantCommunication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsultantCommunication",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "ConsultantVendorReachOuts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "ConsultantVendorReachOuts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorResponseNotes",
                table: "ConsultantVendorReachOuts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsultantCommunication",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "ConsultantVendorReachOuts");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "ConsultantVendorReachOuts");

            migrationBuilder.DropColumn(
                name: "VendorResponseNotes",
                table: "ConsultantVendorReachOuts");
        }
    }
}
