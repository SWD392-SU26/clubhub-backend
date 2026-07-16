using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubHub.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalRevisionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedRevisionAt",
                table: "ClubProposals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedRevisionBy",
                table: "ClubProposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResubmittedAt",
                table: "ClubProposals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisionNote",
                table: "ClubProposals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubProposals_RequestedRevisionBy",
                table: "ClubProposals",
                column: "RequestedRevisionBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubProposals_Users_RequestedRevisionBy",
                table: "ClubProposals",
                column: "RequestedRevisionBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubProposals_Users_RequestedRevisionBy",
                table: "ClubProposals");

            migrationBuilder.DropIndex(
                name: "IX_ClubProposals_RequestedRevisionBy",
                table: "ClubProposals");

            migrationBuilder.DropColumn(
                name: "RequestedRevisionAt",
                table: "ClubProposals");

            migrationBuilder.DropColumn(
                name: "RequestedRevisionBy",
                table: "ClubProposals");

            migrationBuilder.DropColumn(
                name: "ResubmittedAt",
                table: "ClubProposals");

            migrationBuilder.DropColumn(
                name: "RevisionNote",
                table: "ClubProposals");
        }
    }
}
