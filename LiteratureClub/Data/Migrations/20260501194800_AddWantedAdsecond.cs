using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiteratureClub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWantedAdsecond : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WantedAds");

            migrationBuilder.RenameColumn(
                name: "DatePosted",
                table: "WantedAds",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "WantedAds",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ISBN",
                table: "WantedAds",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Edition",
                table: "WantedAds",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "WantedAds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "WantedAds",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CourseCodeId",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredCondition",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreferredFormat",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PublicationYear",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "WantedAds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequesterId",
                table: "WantedAds",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WantedAds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WantedAds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WantedAds_CategoryId",
                table: "WantedAds",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WantedAds_CourseCodeId",
                table: "WantedAds",
                column: "CourseCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WantedAds_RequesterId",
                table: "WantedAds",
                column: "RequesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_WantedAds_AspNetUsers_RequesterId",
                table: "WantedAds",
                column: "RequesterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WantedAds_CourseCodes_CourseCodeId",
                table: "WantedAds",
                column: "CourseCodeId",
                principalTable: "CourseCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WantedAds_TextbookCategories_CategoryId",
                table: "WantedAds",
                column: "CategoryId",
                principalTable: "TextbookCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WantedAds_AspNetUsers_RequesterId",
                table: "WantedAds");

            migrationBuilder.DropForeignKey(
                name: "FK_WantedAds_CourseCodes_CourseCodeId",
                table: "WantedAds");

            migrationBuilder.DropForeignKey(
                name: "FK_WantedAds_TextbookCategories_CategoryId",
                table: "WantedAds");

            migrationBuilder.DropIndex(
                name: "IX_WantedAds_CategoryId",
                table: "WantedAds");

            migrationBuilder.DropIndex(
                name: "IX_WantedAds_CourseCodeId",
                table: "WantedAds");

            migrationBuilder.DropIndex(
                name: "IX_WantedAds_RequesterId",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "CourseCodeId",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "PreferredCondition",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "PreferredFormat",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "PublicationYear",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "RequesterId",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WantedAds");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WantedAds");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "WantedAds",
                newName: "DatePosted");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "ISBN",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Edition",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "WantedAds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WantedAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
