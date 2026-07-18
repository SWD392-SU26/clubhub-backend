using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubHub.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyRolesAndOtpAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Rename SystemRole → Role (giữ nguyên data 'Student'/'UniversityAdmin') ──
            migrationBuilder.RenameColumn(
                name: "SystemRole",
                table: "Users",
                newName: "Role");

            // ── Rename PasswordResetToken → PasswordResetOtp ──────────────────────────
            migrationBuilder.RenameColumn(
                name: "PasswordResetTokenExpiry",
                table: "Users",
                newName: "PasswordResetOtpExpiry");

            migrationBuilder.RenameColumn(
                name: "PasswordResetToken",
                table: "Users",
                newName: "PasswordResetOtp");

            // ── Rename IsActive (bit) → IsEmailVerified ───────────────────────────────
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Users",
                newName: "IsEmailVerified");

            // ── Thêm cột OTP verify email ─────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "EmailVerifyOtp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifyOtpExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            // ── Thêm cột Status (UserStatus enum) với default 'Active' ────────────────
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "Active");

            // ── Đặt IsEmailVerified = true cho tất cả users hiện có ───────────────────
            migrationBuilder.Sql("UPDATE [Users] SET [IsEmailVerified] = 1 WHERE [IsEmailVerified] = 0 OR [IsEmailVerified] IS NULL");

            // ── Cập nhật Role: 'ClubAdmin' cho các user đang là admin trong CLB ────────
            // (Giữ nguyên: Student vẫn là Student, UniversityAdmin vẫn là UniversityAdmin)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerifyOtp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifyOtpExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "SystemRole");

            migrationBuilder.RenameColumn(
                name: "PasswordResetOtpExpiry",
                table: "Users",
                newName: "PasswordResetTokenExpiry");

            migrationBuilder.RenameColumn(
                name: "PasswordResetOtp",
                table: "Users",
                newName: "PasswordResetToken");

            migrationBuilder.RenameColumn(
                name: "IsEmailVerified",
                table: "Users",
                newName: "IsActive");
        }
    }
}
