using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportTools.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitApotheekAtcRelatie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApotheekATCs_ApotheekId",
                table: "ApotheekATCs",
                column: "ApotheekId");

            migrationBuilder.CreateIndex(
                name: "IX_ApotheekATCs_ATCCodeId",
                table: "ApotheekATCs",
                column: "ATCCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApotheekATCs_ATCCodes_ATCCodeId",
                table: "ApotheekATCs",
                column: "ATCCodeId",
                principalTable: "ATCCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApotheekATCs_Apotheken_ApotheekId",
                table: "ApotheekATCs",
                column: "ApotheekId",
                principalTable: "Apotheken",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApotheekATCs_ATCCodes_ATCCodeId",
                table: "ApotheekATCs");

            migrationBuilder.DropForeignKey(
                name: "FK_ApotheekATCs_Apotheken_ApotheekId",
                table: "ApotheekATCs");

            migrationBuilder.DropIndex(
                name: "IX_ApotheekATCs_ApotheekId",
                table: "ApotheekATCs");

            migrationBuilder.DropIndex(
                name: "IX_ApotheekATCs_ATCCodeId",
                table: "ApotheekATCs");
        }
    }
}
