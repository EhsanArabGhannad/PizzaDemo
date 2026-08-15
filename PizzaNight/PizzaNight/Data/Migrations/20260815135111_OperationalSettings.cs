using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaNight.Data.Migrations
{
    /// <inheritdoc />
    public partial class OperationalSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostcodePrefix = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopOpeningHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayOfWeek = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CloseMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOpeningHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AcceptingOnlineOrders = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseOpeningHours = table.Column<bool>(type: "INTEGER", nullable: false),
                    TemporaryClosureMessage = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    DeliveryMinimumPence = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryFeePence = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceFeePence = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryEtaMinMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryEtaMaxMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionEtaMinMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionEtaMaxMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_PostcodePrefix",
                table: "DeliveryZones",
                column: "PostcodePrefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopOpeningHours_DayOfWeek",
                table: "ShopOpeningHours",
                column: "DayOfWeek",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryZones");

            migrationBuilder.DropTable(
                name: "ShopOpeningHours");

            migrationBuilder.DropTable(
                name: "ShopSettings");
        }
    }
}
