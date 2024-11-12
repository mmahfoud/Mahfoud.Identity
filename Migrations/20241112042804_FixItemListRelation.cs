using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mahfoud.Identity.Migrations
{
    /// <inheritdoc />
    public partial class FixItemListRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_to_do_items_to_do_lists_to_do_list_id",
                table: "to_do_items");

            migrationBuilder.DropIndex(
                name: "ix_to_do_items_to_do_list_id",
                table: "to_do_items");

            migrationBuilder.DropColumn(
                name: "to_do_list_id",
                table: "to_do_items");

            migrationBuilder.CreateIndex(
                name: "ix_to_do_items_list_id",
                table: "to_do_items",
                column: "list_id");

            migrationBuilder.AddForeignKey(
                name: "fk_to_do_items_to_do_lists_list_id",
                table: "to_do_items",
                column: "list_id",
                principalTable: "to_do_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_to_do_items_to_do_lists_list_id",
                table: "to_do_items");

            migrationBuilder.DropIndex(
                name: "ix_to_do_items_list_id",
                table: "to_do_items");

            migrationBuilder.AddColumn<long>(
                name: "to_do_list_id",
                table: "to_do_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_to_do_items_to_do_list_id",
                table: "to_do_items",
                column: "to_do_list_id");

            migrationBuilder.AddForeignKey(
                name: "fk_to_do_items_to_do_lists_to_do_list_id",
                table: "to_do_items",
                column: "to_do_list_id",
                principalTable: "to_do_lists",
                principalColumn: "id");
        }
    }
}
