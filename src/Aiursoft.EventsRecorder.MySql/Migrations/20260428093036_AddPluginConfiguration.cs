using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.EventsRecorder.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PluginConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PluginId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventTypeId = table.Column<int>(type: "int", nullable: false),
                    NumericFieldId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginConfigurations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PluginConfigurations_EventFields_NumericFieldId",
                        column: x => x.NumericFieldId,
                        principalTable: "EventFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PluginConfigurations_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PluginConfigurations_EventTypeId",
                table: "PluginConfigurations",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginConfigurations_NumericFieldId",
                table: "PluginConfigurations",
                column: "NumericFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginConfigurations_UserId",
                table: "PluginConfigurations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginConfigurations");
        }
    }
}
