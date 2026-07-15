using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPointAdjustmentActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdjustedByUserId",
                table: "PointTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_AdjustedByUserId",
                table: "PointTransactions",
                column: "AdjustedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_Users_AdjustedByUserId",
                table: "PointTransactions",
                column: "AdjustedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_Users_AdjustedByUserId",
                table: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PointTransactions_AdjustedByUserId",
                table: "PointTransactions");

            migrationBuilder.DropColumn(
                name: "AdjustedByUserId",
                table: "PointTransactions");
        }
    }
}
