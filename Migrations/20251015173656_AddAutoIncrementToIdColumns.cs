using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoIncrementToIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Додаємо auto-increment (SERIAL) для всіх Id колонок в PostgreSQL
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"Appeals_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"Appeals_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"Appeals_Id_seq\" OWNED BY \"Appeals\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"Appeals_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"Appeals\";");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"News_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"News_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"News_Id_seq\" OWNED BY \"News\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"News_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"News\";");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"AppealMessages_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"AppealMessages\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"AppealMessages_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"AppealMessages_Id_seq\" OWNED BY \"AppealMessages\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"AppealMessages_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"AppealMessages\";");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"ContactInfo_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"ContactInfo\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"ContactInfo_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"ContactInfo_Id_seq\" OWNED BY \"ContactInfo\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"ContactInfo_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"ContactInfo\";");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"PartnersInfo_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"PartnersInfo\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"PartnersInfo_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"PartnersInfo_Id_seq\" OWNED BY \"PartnersInfo\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"PartnersInfo_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"PartnersInfo\";");

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS \"EventsInfo_Id_seq\" START WITH 1;");
            migrationBuilder.Sql("ALTER TABLE \"EventsInfo\" ALTER COLUMN \"Id\" SET DEFAULT nextval('\"EventsInfo_Id_seq\"');");
            migrationBuilder.Sql("ALTER SEQUENCE \"EventsInfo_Id_seq\" OWNED BY \"EventsInfo\".\"Id\";");
            migrationBuilder.Sql("SELECT setval('\"EventsInfo_Id_seq\"', COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"EventsInfo\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Видаляємо auto-increment (SERIAL) для всіх Id колонок
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"Appeals_Id_seq\";");

            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"News_Id_seq\";");

            migrationBuilder.Sql("ALTER TABLE \"AppealMessages\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"AppealMessages_Id_seq\";");

            migrationBuilder.Sql("ALTER TABLE \"ContactInfo\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"ContactInfo_Id_seq\";");

            migrationBuilder.Sql("ALTER TABLE \"PartnersInfo\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"PartnersInfo_Id_seq\";");

            migrationBuilder.Sql("ALTER TABLE \"EventsInfo\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"EventsInfo_Id_seq\";");
        }
    }
}
