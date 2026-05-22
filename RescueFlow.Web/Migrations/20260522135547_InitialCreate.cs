using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RescueFlow.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlgorithmRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeverityScore = table.Column<int>(type: "int", nullable: false),
                    RequiredVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelectedVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectedVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelectedHospitalId = table.Column<int>(type: "int", nullable: true),
                    RejectedHospitalsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    ValidationErrors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionTrace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlgorithmRunLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchDecisionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchDecisionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hospitals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hospitals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RescueCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReporterPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IncidentCategory = table.Column<int>(type: "int", nullable: false),
                    InjuryType = table.Column<int>(type: "int", nullable: false),
                    NumberOfPatients = table.Column<int>(type: "int", nullable: false),
                    HasSevereBleeding = table.Column<bool>(type: "bit", nullable: false),
                    HasBreathingProblem = table.Column<bool>(type: "bit", nullable: false),
                    IsUnconscious = table.Column<bool>(type: "bit", nullable: false),
                    HasFire = table.Column<bool>(type: "bit", nullable: false),
                    HasTrappedVictim = table.Column<bool>(type: "bit", nullable: false),
                    HasChemicalRisk = table.Column<bool>(type: "bit", nullable: false),
                    HasExplosionRisk = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedSeverity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SafetyValidationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: false),
                    ValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckedRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyValidationResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SafetyViolations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ViolationDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyViolations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HospitalCapabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HospitalId = table.Column<int>(type: "int", nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalCapabilities_Hospitals_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HospitalCapacities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HospitalId = table.Column<int>(type: "int", nullable: false),
                    EmergencyCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentEmergencyLoad = table.Column<int>(type: "int", nullable: false),
                    TraumaCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentTraumaLoad = table.Column<int>(type: "int", nullable: false),
                    IcuCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentIcuLoad = table.Column<int>(type: "int", nullable: false),
                    BurnCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentBurnLoad = table.Column<int>(type: "int", nullable: false),
                    ToxicologyCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentToxicologyLoad = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalCapacities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalCapacities_Hospitals_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponsePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RescueCaseId = table.Column<int>(type: "int", nullable: false),
                    SelectedHospitalId = table.Column<int>(type: "int", nullable: true),
                    SeverityLevel = table.Column<int>(type: "int", nullable: false),
                    RequiresHospital = table.Column<bool>(type: "bit", nullable: false),
                    SafetyValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedSceneEtaMinutes = table.Column<double>(type: "float", nullable: false),
                    EstimatedHospitalEtaMinutes = table.Column<double>(type: "float", nullable: true),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelectedVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectedVehiclesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectedHospitalsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsePlans_Hospitals_SelectedHospitalId",
                        column: x => x.SelectedHospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResponsePlans_RescueCases_RescueCaseId",
                        column: x => x.RescueCaseId,
                        principalTable: "RescueCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RescueVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaxCrewCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentCrewCount = table.Column<int>(type: "int", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: false),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RescueVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RescueVehicles_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResponseAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResponsePlanId = table.Column<int>(type: "int", nullable: false),
                    RescueVehicleId = table.Column<int>(type: "int", nullable: false),
                    EtaMinutes = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseAssignments_RescueVehicles_RescueVehicleId",
                        column: x => x.RescueVehicleId,
                        principalTable: "RescueVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResponseAssignments_ResponsePlans_ResponsePlanId",
                        column: x => x.ResponsePlanId,
                        principalTable: "ResponsePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleEquipment",
                columns: table => new
                {
                    RescueVehicleId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleEquipment", x => new { x.RescueVehicleId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_VehicleEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleEquipment_RescueVehicles_RescueVehicleId",
                        column: x => x.RescueVehicleId,
                        principalTable: "RescueVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalCapabilities_HospitalId",
                table: "HospitalCapabilities",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalCapacities_HospitalId",
                table: "HospitalCapacities",
                column: "HospitalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RescueVehicles_StationId",
                table: "RescueVehicles",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseAssignments_RescueVehicleId",
                table: "ResponseAssignments",
                column: "RescueVehicleId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseAssignments_ResponsePlanId",
                table: "ResponseAssignments",
                column: "ResponsePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsePlans_RescueCaseId",
                table: "ResponsePlans",
                column: "RescueCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsePlans_SelectedHospitalId",
                table: "ResponsePlans",
                column: "SelectedHospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEquipment_EquipmentId",
                table: "VehicleEquipment",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlgorithmRunLogs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DispatchDecisionLogs");

            migrationBuilder.DropTable(
                name: "HospitalCapabilities");

            migrationBuilder.DropTable(
                name: "HospitalCapacities");

            migrationBuilder.DropTable(
                name: "ResponseAssignments");

            migrationBuilder.DropTable(
                name: "SafetyValidationResults");

            migrationBuilder.DropTable(
                name: "SafetyViolations");

            migrationBuilder.DropTable(
                name: "VehicleEquipment");

            migrationBuilder.DropTable(
                name: "ResponsePlans");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "RescueVehicles");

            migrationBuilder.DropTable(
                name: "Hospitals");

            migrationBuilder.DropTable(
                name: "RescueCases");

            migrationBuilder.DropTable(
                name: "Stations");
        }
    }
}
