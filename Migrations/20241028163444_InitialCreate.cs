using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataVisualizations",
                columns: table => new
                {
                    VisualizationRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VisualizationDataFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataVisualizations", x => x.VisualizationRequestID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Algorithms",
                columns: table => new
                {
                    AlgorithmID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<string>(type: "varchar(36)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlgorithmName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainClassName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlgorithmType = table.Column<int>(type: "int", nullable: false),
                    JarFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Algorithms", x => x.AlgorithmID);
                    table.ForeignKey(
                        name: "FK_Algorithms_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StoredDataSets",
                columns: table => new
                {
                    DataSetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<string>(type: "varchar(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredDataSets", x => x.DataSetID);
                    table.ForeignKey(
                        name: "FK_StoredDataSets_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    UploadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserID = table.Column<string>(type: "varchar(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalChunks = table.Column<int>(type: "int", nullable: false),
                    UploadedChunks = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Completed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.UploadId);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlgorithmParameters",
                columns: table => new
                {
                    ParameterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlgorithmID = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DriverIndex = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlgorithmParameters", x => x.ParameterID);
                    table.ForeignKey(
                        name: "FK_AlgorithmParameters_Algorithms_AlgorithmID",
                        column: x => x.AlgorithmID,
                        principalTable: "Algorithms",
                        principalColumn: "AlgorithmID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExperimentRequests",
                columns: table => new
                {
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserID = table.Column<string>(type: "varchar(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlgorithmID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Parameters = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentRequests", x => x.ExperimentID);
                    table.ForeignKey(
                        name: "FK_ExperimentRequests_Algorithms_AlgorithmID",
                        column: x => x.AlgorithmID,
                        principalTable: "Algorithms",
                        principalColumn: "AlgorithmID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperimentRequests_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AlgorithmRequestParameters",
                columns: table => new
                {
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DatasetName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlgorithmRequestParameters", x => x.ExperimentID);
                    table.ForeignKey(
                        name: "FK_AlgorithmRequestParameters_ExperimentRequests_ExperimentID",
                        column: x => x.ExperimentID,
                        principalTable: "ExperimentRequests",
                        principalColumn: "ExperimentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClusterParameters",
                columns: table => new
                {
                    ClusterParamID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NodeCount = table.Column<int>(type: "int", nullable: false),
                    DriverMemory = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DriverCores = table.Column<int>(type: "int", nullable: false),
                    ExecutorNumber = table.Column<int>(type: "int", nullable: false),
                    ExecutorCores = table.Column<int>(type: "int", nullable: false),
                    ExecutorMemory = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MemoryOverhead = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterParameters", x => x.ClusterParamID);
                    table.ForeignKey(
                        name: "FK_ClusterParameters_ExperimentRequests_ExperimentID",
                        column: x => x.ExperimentID,
                        principalTable: "ExperimentRequests",
                        principalColumn: "ExperimentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExperimentResults",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CSVFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CSVFileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MetaDataFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentResults", x => x.ResultID);
                    table.ForeignKey(
                        name: "FK_ExperimentResults_ExperimentRequests_ExperimentID",
                        column: x => x.ExperimentID,
                        principalTable: "ExperimentRequests",
                        principalColumn: "ExperimentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VisualizationExperiments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VisualizationRequestID = table.Column<int>(type: "int", nullable: false),
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualizationExperiments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_VisualizationExperiments_DataVisualizations_VisualizationReq~",
                        column: x => x.VisualizationRequestID,
                        principalTable: "DataVisualizations",
                        principalColumn: "VisualizationRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisualizationExperiments_ExperimentRequests_ExperimentID",
                        column: x => x.ExperimentID,
                        principalTable: "ExperimentRequests",
                        principalColumn: "ExperimentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExperimentAlgorithmParameterValues",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExperimentID = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterID = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlgorithmRequestParametersExperimentID = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentAlgorithmParameterValues", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExperimentAlgorithmParameterValues_AlgorithmParameters_Param~",
                        column: x => x.ParameterID,
                        principalTable: "AlgorithmParameters",
                        principalColumn: "ParameterID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperimentAlgorithmParameterValues_AlgorithmRequestParameter~",
                        column: x => x.AlgorithmRequestParametersExperimentID,
                        principalTable: "AlgorithmRequestParameters",
                        principalColumn: "ExperimentID");
                    table.ForeignKey(
                        name: "FK_ExperimentAlgorithmParameterValues_ExperimentRequests_Experi~",
                        column: x => x.ExperimentID,
                        principalTable: "ExperimentRequests",
                        principalColumn: "ExperimentID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AlgorithmParameters_AlgorithmID",
                table: "AlgorithmParameters",
                column: "AlgorithmID");

            migrationBuilder.CreateIndex(
                name: "IX_Algorithms_UserID",
                table: "Algorithms",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterParameters_ExperimentID",
                table: "ClusterParameters",
                column: "ExperimentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentAlgorithmParameterValues_AlgorithmRequestParameter~",
                table: "ExperimentAlgorithmParameterValues",
                column: "AlgorithmRequestParametersExperimentID");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentAlgorithmParameterValues_ExperimentID",
                table: "ExperimentAlgorithmParameterValues",
                column: "ExperimentID");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentAlgorithmParameterValues_ParameterID",
                table: "ExperimentAlgorithmParameterValues",
                column: "ParameterID");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentRequests_AlgorithmID",
                table: "ExperimentRequests",
                column: "AlgorithmID");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentRequests_UserID",
                table: "ExperimentRequests",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentResults_ExperimentID",
                table: "ExperimentResults",
                column: "ExperimentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredDataSets_UserID",
                table: "StoredDataSets",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_UserID",
                table: "UploadSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_VisualizationExperiments_ExperimentID",
                table: "VisualizationExperiments",
                column: "ExperimentID");

            migrationBuilder.CreateIndex(
                name: "IX_VisualizationExperiments_VisualizationRequestID",
                table: "VisualizationExperiments",
                column: "VisualizationRequestID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterParameters");

            migrationBuilder.DropTable(
                name: "ExperimentAlgorithmParameterValues");

            migrationBuilder.DropTable(
                name: "ExperimentResults");

            migrationBuilder.DropTable(
                name: "StoredDataSets");

            migrationBuilder.DropTable(
                name: "UploadSessions");

            migrationBuilder.DropTable(
                name: "VisualizationExperiments");

            migrationBuilder.DropTable(
                name: "AlgorithmParameters");

            migrationBuilder.DropTable(
                name: "AlgorithmRequestParameters");

            migrationBuilder.DropTable(
                name: "DataVisualizations");

            migrationBuilder.DropTable(
                name: "ExperimentRequests");

            migrationBuilder.DropTable(
                name: "Algorithms");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
