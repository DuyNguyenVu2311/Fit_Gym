using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitGym.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGateEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaSuKien",
                table: "LICH_SU_CHECKIN",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LICH_SU_CHECKIN_MaSuKien",
                table: "LICH_SU_CHECKIN",
                column: "MaSuKien",
                unique: true,
                filter: "[MaSuKien] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LICH_SU_CHECKIN_MaSuKien",
                table: "LICH_SU_CHECKIN");

            migrationBuilder.DropColumn(
                name: "MaSuKien",
                table: "LICH_SU_CHECKIN");
        }
    }
}
