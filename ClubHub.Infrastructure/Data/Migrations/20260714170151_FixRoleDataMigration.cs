using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubHub.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleDataMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Các user có Role rỗng (do migration trước thêm cột Role với default '') 
            // cần được set lại Role đúng.
            // User admin có email admin@gmail.com → UniversityAdmin
            migrationBuilder.Sql(@"
                UPDATE [Users]
                SET [Role] = 'UniversityAdmin'
                WHERE [Email] = 'admin@gmail.com' AND ([Role] = '' OR [Role] IS NULL);

                -- Các user còn lại nếu Role rỗng → mặc định Student
                UPDATE [Users]
                SET [Role] = 'Student'
                WHERE [Role] = '' OR [Role] IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không cần rollback data
        }
    }
}
