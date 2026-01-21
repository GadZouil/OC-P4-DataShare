using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataShare.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeOwnerIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_AspNetUsers_OwnerId",
                table: "Files");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Files",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_AspNetUsers_OwnerId",
                table: "Files",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_AspNetUsers_OwnerId",
                table: "Files");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_AspNetUsers_OwnerId",
                table: "Files",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
