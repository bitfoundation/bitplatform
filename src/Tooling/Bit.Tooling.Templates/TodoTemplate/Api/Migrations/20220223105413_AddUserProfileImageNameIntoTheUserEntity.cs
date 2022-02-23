using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoTemplate.Api.Migrations
{
    public partial class AddUserProfileImageNameIntoTheUserEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageName",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageName",
                table: "AspNetUsers");
        }
    }
}
