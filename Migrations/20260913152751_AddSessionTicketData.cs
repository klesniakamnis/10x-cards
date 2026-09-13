using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _10x_cards.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTicketData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "TicketData",
                table: "AuthSessions",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketData",
                table: "AuthSessions");
        }
    }
}
