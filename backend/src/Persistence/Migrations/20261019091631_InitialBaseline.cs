using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline: no business tables (M0). Every table and sequence the migration role creates from now
            // on is usable — but not alterable — by the application role (M0-11; implementation-plan §6.4).
            // Roles and the database are created by dev/postgres/init/01-bootstrap.sh.
            migrationBuilder.Sql(
                "ALTER DEFAULT PRIVILEGES FOR ROLE platform_migrator IN SCHEMA public " +
                "GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO platform_app;");
            migrationBuilder.Sql(
                "ALTER DEFAULT PRIVILEGES FOR ROLE platform_migrator IN SCHEMA public " +
                "GRANT USAGE, SELECT ON SEQUENCES TO platform_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER DEFAULT PRIVILEGES FOR ROLE platform_migrator IN SCHEMA public " +
                "REVOKE USAGE, SELECT ON SEQUENCES FROM platform_app;");
            migrationBuilder.Sql(
                "ALTER DEFAULT PRIVILEGES FOR ROLE platform_migrator IN SCHEMA public " +
                "REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM platform_app;");
        }
    }
}
