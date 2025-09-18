using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSContestConnect.Web.Migrations
{
    /// <inheritdoc />
    public partial class EventImageAndTicketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Events",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegisteredCount",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketCapacity",
                table: "Events",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegisteredCount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketCapacity",
                table: "Events");
        }
    }
}
