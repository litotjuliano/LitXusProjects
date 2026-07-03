# company-profile Specification

## Purpose
TBD - created by archiving change baseline-existing-system. Update Purpose after archive.
## Requirements
### Requirement: Company profile captures Malaysia-specific registration details
The system SHALL require `Name`, `SsmRegistrationNumber`, `Tin` (tax identification number), `BusinessType` (one of `PrivateCompany`, `PublicCompany`, `SoleProprietor`, `Partnership`, `Other`), `MsicCode`, `PrincipalBusinessActivity`, financial year end (month 1-12, day 1-31), `AccountingFramework` (one of `Mpers`, `Mfrs`), and a full address (line 1, city, state, postal code, country) plus contact phone/email, before a company profile can be saved.

#### Scenario: Upsert rejects an unrecognized business type
- **WHEN** `PUT /api/v1/company/profile` is submitted with `BusinessType` set to a value other than `PrivateCompany`, `PublicCompany`, `SoleProprietor`, `Partnership`, or `Other`
- **THEN** the request is rejected with a validation error naming the allowed set

#### Scenario: Upsert rejects an invalid financial year end day
- **WHEN** `FinancialYearEndDay` is submitted as `0` or `32`
- **THEN** the request is rejected (must be between 1 and 31 inclusive)

### Requirement: Signatories require identity and contact details
The system SHALL require `Name`, `IcNumber` (identity card number), `Position`, and a valid `Email` before a signatory can be added to a company profile.

#### Scenario: Adding a signatory without an IC number is rejected
- **WHEN** `POST /api/v1/company/signatories` is submitted with an empty `IcNumber`
- **THEN** the request is rejected with a validation error

### Requirement: Company profile access requires explicit permissions
`GET /api/v1/company/profile` and `GET /api/v1/company/signatories` SHALL require the `Company.Profile.Read` permission; `PUT /api/v1/company/profile`, `POST /api/v1/company/signatories`, and `DELETE /api/v1/company/signatories/{id}` SHALL require `Company.Profile.Update`.

#### Scenario: User without Company.Profile.Update cannot modify the profile
- **WHEN** a user lacking the `Company.Profile.Update` permission calls `PUT /api/v1/company/profile`
- **THEN** the request is rejected with a 403 Forbidden response

### Requirement: Company profile feeds the shared report letterhead
The `ReportLetterhead` frontend component SHALL render the current company's name, address, and registration details centered at the top of every financial report (Trial Balance, Income Statement, Balance Sheet, General Ledger), sourced live from `GET /api/v1/company/profile` rather than hard-coded per report.

#### Scenario: Updating the company name reflects on reports without redeploying
- **WHEN** the company name is changed via the Company Profile admin page
- **THEN** the next time any financial report is viewed, its letterhead shows the updated name

