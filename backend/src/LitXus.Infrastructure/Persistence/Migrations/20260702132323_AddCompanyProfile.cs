using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LitXus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SsmRegistrationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Tin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Usid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessType = table.Column<int>(type: "int", nullable: false),
                    MsicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PrincipalBusinessActivity = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstablishmentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinancialYearEndMonth = table.Column<int>(type: "int", nullable: false),
                    FinancialYearEndDay = table.Column<int>(type: "int", nullable: false),
                    AccountingFramework = table.Column<int>(type: "int", nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SecondaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryBankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrimaryBankAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryBankAccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryBankSwiftCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SstRegistrationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EisNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EpfNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SocsoNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ExternalAuditorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanySecretaryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanySignatories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IcNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySignatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySignatories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySignatories_CompanyId",
                table: "CompanySignatories",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySignatories");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
