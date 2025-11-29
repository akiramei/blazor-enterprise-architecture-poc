using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.ApprovalWorkflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aw_Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStepNumber = table.Column<int>(type: "integer", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_ApprovalHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_ApprovalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aw_ApprovalHistory_aw_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "aw_Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aw_WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aw_WorkflowSteps_aw_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "aw_WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aw_Applications_ApplicantId",
                table: "aw_Applications",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_aw_Applications_CreatedAt",
                table: "aw_Applications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_aw_Applications_Status",
                table: "aw_Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_aw_Applications_Type",
                table: "aw_Applications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_aw_Applications_WorkflowDefinitionId",
                table: "aw_Applications",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_aw_ApprovalHistory_ApplicationId",
                table: "aw_ApprovalHistory",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_aw_OutboxMessages_ProcessedOnUtc",
                table: "aw_OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_aw_WorkflowDefinitions_ApplicationType",
                table: "aw_WorkflowDefinitions",
                column: "ApplicationType");

            migrationBuilder.CreateIndex(
                name: "IX_aw_WorkflowDefinitions_ApplicationType_Active",
                table: "aw_WorkflowDefinitions",
                columns: new[] { "ApplicationType", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_aw_WorkflowDefinitions_IsActive",
                table: "aw_WorkflowDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aw_WorkflowSteps_DefinitionId_StepNumber",
                table: "aw_WorkflowSteps",
                columns: new[] { "WorkflowDefinitionId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aw_ApprovalHistory");

            migrationBuilder.DropTable(
                name: "aw_OutboxMessages");

            migrationBuilder.DropTable(
                name: "aw_WorkflowSteps");

            migrationBuilder.DropTable(
                name: "aw_Applications");

            migrationBuilder.DropTable(
                name: "aw_WorkflowDefinitions");
        }
    }
}
