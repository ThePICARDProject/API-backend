using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Backend.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedNamesAndAddedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ID",
                table: "VisualizationExperiments",
                newName: "VisualizationExperimentID");

            migrationBuilder.RenameColumn(
                name: "CSVFilePath",
                table: "ExperimentResults",
                newName: "ResultFilePath");

            migrationBuilder.RenameColumn(
                name: "CSVFileName",
                table: "ExperimentResults",
                newName: "ResultFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VisualizationExperimentID",
                table: "VisualizationExperiments",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "ResultFilePath",
                table: "ExperimentResults",
                newName: "CSVFilePath");

            migrationBuilder.RenameColumn(
                name: "ResultFileName",
                table: "ExperimentResults",
                newName: "CSVFileName");
        }
    }
}
