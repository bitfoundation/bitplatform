using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoTemplate.Api.Migrations
{
    public partial class AddEmailLastTimeSentFieldsToTheUserEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmationEmailLastTimeSent",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResetPasswordEmailLastTimeSent",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationEmailLastTimeSent",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ResetPasswordEmailLastTimeSent",
                table: "AspNetUsers");
        }
    }
}
