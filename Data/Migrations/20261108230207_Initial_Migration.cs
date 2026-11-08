using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Studium_Catalog.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MapPositionX = table.Column<double>(type: "double precision", nullable: true),
                    MapPositionY = table.Column<double>(type: "double precision", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.CheckConstraint("CK_Rooms_Capacity", "\"Capacity\" BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_Rooms_MapPositionX", "\"MapPositionX\" IS NULL OR \"MapPositionX\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Rooms_MapPositionY", "\"MapPositionY\" IS NULL OR \"MapPositionY\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Rooms_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilitySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySchedules", x => x.Id);
                    table.CheckConstraint("CK_AvailabilitySchedules_Window", "\"ClosesAt\" > \"OpensAt\"");
                    table.ForeignKey(
                        name: "FK_AvailabilitySchedules_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceBlocks", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceBlocks_Period", "\"EndsAtUtc\" > \"StartsAtUtc\"");
                    table.ForeignKey(
                        name: "FK_MaintenanceBlocks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceItems", x => x.Id);
                    table.CheckConstraint("CK_ResourceItems_Quantity", "\"Quantity\" >= 1");
                    table.ForeignKey(
                        name: "FK_ResourceItems_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySchedules_RoomId_DayOfWeek",
                table: "AvailabilitySchedules",
                columns: new[] { "RoomId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Code",
                table: "Buildings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceBlocks_RoomId_EndsAtUtc",
                table: "MaintenanceBlocks",
                columns: new[] { "RoomId", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_RoomId",
                table: "ResourceItems",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_Type",
                table: "ResourceItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingId_Code",
                table: "Rooms",
                columns: new[] { "BuildingId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingId_Floor",
                table: "Rooms",
                columns: new[] { "BuildingId", "Floor" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_IsActive",
                table: "Rooms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Type",
                table: "Rooms",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilitySchedules");

            migrationBuilder.DropTable(
                name: "MaintenanceBlocks");

            migrationBuilder.DropTable(
                name: "ResourceItems");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
