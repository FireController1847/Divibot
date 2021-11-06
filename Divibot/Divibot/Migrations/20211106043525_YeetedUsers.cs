using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Divibot.Migrations {

    public partial class YeetedUsers : Migration {

        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "YeetedUsers",
                columns: table => new {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_YeetedUsers", x => new { x.GuildId, x.ChannelId, x.UserId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "YeetedUsers");
        }

    }

}
