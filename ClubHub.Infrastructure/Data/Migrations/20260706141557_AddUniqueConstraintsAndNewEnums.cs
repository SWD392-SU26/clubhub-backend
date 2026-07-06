using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubHub.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsAndNewEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClubMembers_UserId_ClubId",
                table: "ClubMembers");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Name",
                table: "Clubs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_UserId_ClubId",
                table: "ClubMembers",
                columns: new[] { "UserId", "ClubId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clubs_Name",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_ClubMembers_UserId_ClubId",
                table: "ClubMembers");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_UserId_ClubId",
                table: "ClubMembers",
                columns: new[] { "UserId", "ClubId" });
        }
    }
}
