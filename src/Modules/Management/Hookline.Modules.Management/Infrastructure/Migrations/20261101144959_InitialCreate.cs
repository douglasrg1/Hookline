using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hookline.Modules.Management.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "management");

            migrationBuilder.CreateTable(
                name: "consumers",
                schema: "management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consumers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consumer_keys",
                schema: "management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    display_prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consumer_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_consumer_keys_consumers_consumer_id",
                        column: x => x.consumer_id,
                        principalSchema: "management",
                        principalTable: "consumers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endpoints",
                schema: "management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    event_types = table.Column<string[]>(type: "character varying(100)[]", nullable: false),
                    encrypted_secret = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_endpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_endpoints_consumers_consumer_id",
                        column: x => x.consumer_id,
                        principalSchema: "management",
                        principalTable: "consumers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consumer_keys_consumer_id",
                schema: "management",
                table: "consumer_keys",
                column: "consumer_id");

            migrationBuilder.CreateIndex(
                name: "ix_consumer_keys_hash",
                schema: "management",
                table: "consumer_keys",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consumers_external_id",
                schema: "management",
                table: "consumers",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_endpoints_consumer_id_url",
                schema: "management",
                table: "endpoints",
                columns: new[] { "consumer_id", "url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumer_keys",
                schema: "management");

            migrationBuilder.DropTable(
                name: "endpoints",
                schema: "management");

            migrationBuilder.DropTable(
                name: "consumers",
                schema: "management");
        }
    }
}
