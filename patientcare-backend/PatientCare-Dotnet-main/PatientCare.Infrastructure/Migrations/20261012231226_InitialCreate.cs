using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PatientCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Caregivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CaregiverGender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caregivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientGender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManagerCaregiverId = table.Column<int>(type: "integer", nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Caregivers_ManagerCaregiverId",
                        column: x => x.ManagerCaregiverId,
                        principalTable: "Caregivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RequiredGender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareShifts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    CaregiverId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMembers_Caregivers_CaregiverId",
                        column: x => x.CaregiverId,
                        principalTable: "Caregivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientMembers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareShiftTags",
                columns: table => new
                {
                    CareShiftsId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareShiftTags", x => new { x.CareShiftsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_CareShiftTags_CareShifts_CareShiftsId",
                        column: x => x.CareShiftsId,
                        principalTable: "CareShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareShiftTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CareShiftId = table.Column<int>(type: "integer", nullable: false),
                    CaregiverId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_CareShifts_CareShiftId",
                        column: x => x.CareShiftId,
                        principalTable: "CareShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftAssignments_Caregivers_CaregiverId",
                        column: x => x.CaregiverId,
                        principalTable: "Caregivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestorId = table.Column<int>(type: "integer", nullable: false),
                    TargetCaregiverId = table.Column<int>(type: "integer", nullable: true),
                    CareShiftId = table.Column<int>(type: "integer", nullable: false),
                    TargetCareShiftId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ResponseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_CareShifts_CareShiftId",
                        column: x => x.CareShiftId,
                        principalTable: "CareShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_CareShifts_TargetCareShiftId",
                        column: x => x.TargetCareShiftId,
                        principalTable: "CareShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Caregivers_RequestorId",
                        column: x => x.RequestorId,
                        principalTable: "Caregivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Caregivers_TargetCaregiverId",
                        column: x => x.TargetCaregiverId,
                        principalTable: "Caregivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caregivers_Email",
                table: "Caregivers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareShifts_PatientId",
                table: "CareShifts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CareShiftTags_TagsId",
                table: "CareShiftTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMembers_CaregiverId",
                table: "PatientMembers",
                column: "CaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMembers_PatientId_CaregiverId",
                table: "PatientMembers",
                columns: new[] { "PatientId", "CaregiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_InviteCode",
                table: "Patients",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ManagerCaregiverId",
                table: "Patients",
                column: "ManagerCaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalCode",
                table: "Patients",
                column: "MedicalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_CaregiverId",
                table: "ShiftAssignments",
                column: "CaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_CareShiftId_CaregiverId",
                table: "ShiftAssignments",
                columns: new[] { "CareShiftId", "CaregiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_CareShiftId",
                table: "ShiftSwapRequests",
                column: "CareShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequestorId",
                table: "ShiftSwapRequests",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetCaregiverId",
                table: "ShiftSwapRequests",
                column: "TargetCaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetCareShiftId",
                table: "ShiftSwapRequests",
                column: "TargetCareShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareShiftTags");

            migrationBuilder.DropTable(
                name: "PatientMembers");

            migrationBuilder.DropTable(
                name: "ShiftAssignments");

            migrationBuilder.DropTable(
                name: "ShiftSwapRequests");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "CareShifts");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Caregivers");
        }
    }
}
