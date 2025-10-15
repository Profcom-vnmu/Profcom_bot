using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentUnionBot.Migrations
{
    /// <inheritdoc />
    public partial class FixDateTimeColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Конвертуємо text колонки DateTime в timestamp для PostgreSQL
            
            // Appeals таблиця
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"CreatedAt\" TYPE timestamp USING \"CreatedAt\"::timestamp;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"ClosedAt\" TYPE timestamp USING CASE WHEN \"ClosedAt\" IS NOT NULL THEN \"ClosedAt\"::timestamp ELSE NULL END;");
            
            // AppealMessages таблиця
            migrationBuilder.Sql("ALTER TABLE \"AppealMessages\" ALTER COLUMN \"SentAt\" TYPE timestamp USING \"SentAt\"::timestamp;");
            
            // Users таблиця
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"JoinedAt\" TYPE timestamp USING \"JoinedAt\"::timestamp;");
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"ProfileUpdatedAt\" TYPE timestamp USING CASE WHEN \"ProfileUpdatedAt\" IS NOT NULL THEN \"ProfileUpdatedAt\"::timestamp ELSE NULL END;");
            
            // News таблиця
            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"CreatedAt\" TYPE timestamp USING \"CreatedAt\"::timestamp;");
            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"PublishAt\" TYPE timestamp USING CASE WHEN \"PublishAt\" IS NOT NULL THEN \"PublishAt\"::timestamp ELSE NULL END;");
            
            // ContactInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"ContactInfo\" ALTER COLUMN \"UpdatedAt\" TYPE timestamp USING \"UpdatedAt\"::timestamp;");
            
            // PartnersInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"PartnersInfo\" ALTER COLUMN \"UpdatedAt\" TYPE timestamp USING \"UpdatedAt\"::timestamp;");
            
            // EventsInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"EventsInfo\" ALTER COLUMN \"UpdatedAt\" TYPE timestamp USING \"UpdatedAt\"::timestamp;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Повертаємо timestamp колонки в text (може втратити точність!)
            
            // Appeals таблиця
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"CreatedAt\" TYPE text USING \"CreatedAt\"::text;");
            migrationBuilder.Sql("ALTER TABLE \"Appeals\" ALTER COLUMN \"ClosedAt\" TYPE text USING CASE WHEN \"ClosedAt\" IS NOT NULL THEN \"ClosedAt\"::text ELSE NULL END;");
            
            // AppealMessages таблиця
            migrationBuilder.Sql("ALTER TABLE \"AppealMessages\" ALTER COLUMN \"SentAt\" TYPE text USING \"SentAt\"::text;");
            
            // Users таблиця
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"JoinedAt\" TYPE text USING \"JoinedAt\"::text;");
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"ProfileUpdatedAt\" TYPE text USING CASE WHEN \"ProfileUpdatedAt\" IS NOT NULL THEN \"ProfileUpdatedAt\"::text ELSE NULL END;");
            
            // News таблиця
            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"CreatedAt\" TYPE text USING \"CreatedAt\"::text;");
            migrationBuilder.Sql("ALTER TABLE \"News\" ALTER COLUMN \"PublishAt\" TYPE text USING CASE WHEN \"PublishAt\" IS NOT NULL THEN \"PublishAt\"::text ELSE NULL END;");
            
            // ContactInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"ContactInfo\" ALTER COLUMN \"UpdatedAt\" TYPE text USING \"UpdatedAt\"::text;");
            
            // PartnersInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"PartnersInfo\" ALTER COLUMN \"UpdatedAt\" TYPE text USING \"UpdatedAt\"::text;");
            
            // EventsInfo таблиця
            migrationBuilder.Sql("ALTER TABLE \"EventsInfo\" ALTER COLUMN \"UpdatedAt\" TYPE text USING \"UpdatedAt\"::text;");
        }
    }
}
