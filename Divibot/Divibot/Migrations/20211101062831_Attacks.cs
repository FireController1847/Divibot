using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Divibot.Migrations {

    public partial class Attacks : Migration {

        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "AttackTypeChances",
                columns: table => new {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AttackCategory = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttackTypeId = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CritChance = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    Chance = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    IneffChance = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AttackTypeChances", x => new { x.UserId, x.AttackCategory, x.AttackTypeId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttackUsers",
                columns: table => new {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Class = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AttackUsers", x => x.UserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomAttackCategoryChances",
                columns: table => new {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Category = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChanceMin = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    ChanceMax = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_CustomAttackCategoryChances", x => new { x.UserId, x.Category });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomAttackModifierChances",
                columns: table => new {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Modifier = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChanceMin = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false),
                    ChanceMax = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_CustomAttackModifierChances", x => new { x.UserId, x.Modifier });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(name: "AttackTypeChances");
            migrationBuilder.DropTable(name: "AttackUsers");
            migrationBuilder.DropTable(name: "CustomAttackCategoryChances");
            migrationBuilder.DropTable(name: "CustomAttackModifierChances");
        }

    }

}
