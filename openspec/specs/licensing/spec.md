# licensing Specification

## Purpose
TBD - created by archiving change baseline-existing-system. Update Purpose after archive.
## Requirements
### Requirement: License keys are signed offline and verified via RS256
The system SHALL only accept license keys that are valid RS256-signed JWTs, signed by the private key of a keypair generated offline via `backend/tools/LitXus.LicenseGenerator` (a dependency-free console tool, never shipped with a deployment). The deployed app SHALL hold only the public key half (`Licensing:PublicKeyPem` in `appsettings.json`) and MUST NOT be able to sign new license keys itself.

#### Scenario: Valid key is accepted
- **WHEN** a Super Admin submits a license token signed with the private key matching the deployment's configured `Licensing:PublicKeyPem`
- **THEN** the system verifies the signature, extracts `productCode`/`company`/`module`/`iat`/`nbf`/`exp` claims, and applies them

#### Scenario: Key signed by a different keypair is rejected
- **WHEN** a license token is signed with a private key whose public half does not match the deployment's configured `Licensing:PublicKeyPem`
- **THEN** the system rejects it with `INVALID_LICENSE_KEY: "the signature is invalid or the key is malformed."`

#### Scenario: Expired key is rejected
- **WHEN** a license token's `exp` claim is in the past relative to the server's clock (beyond a 5-minute clock-skew allowance)
- **THEN** the system rejects it with `INVALID_LICENSE_KEY: "this key has expired."`

#### Scenario: Token missing required claims is rejected
- **WHEN** a validly-signed JWT lacks a `productCode` or `company` claim (i.e. wasn't produced by the LitXus license generator)
- **THEN** the system rejects it with `INVALID_LICENSE_KEY: "missing productCode claim."` or `"missing company claim."` respectively

### Requirement: Applying a license key atomically replaces all license state
`POST /api/v1/admin/license/apply-key` SHALL verify the submitted key, then atomically replace `ProductCode`, `IssuedToCompany`, `EnabledModules`, `IssuedAtUtc`, and `ExpiresAtUtc` on the current `License` row from the token's claims in one operation (`License.ApplyVerifiedKey`), and SHALL invalidate the cached feature-flag state (`IFeatureFlagService.InvalidateAsync()`) so the new module set takes effect immediately for every logged-in user without an app restart.

#### Scenario: Applying a new key replaces the enabled module set, not merges it
- **WHEN** the current license has `Accounting` enabled and a new key is applied with only `Sales,Inventory` in its `module` claims
- **THEN** the resulting `EnabledModules` is exactly `Sales,Inventory` — `Accounting` is no longer enabled

### Requirement: License management is restricted to Super Admin
`GET /api/v1/admin/license` SHALL require the `Admin.License.Read` permission and `POST /api/v1/admin/license/apply-key` SHALL require `Admin.License.Update`; both permissions SHALL be granted only to the Super Admin role (excluded from the Admin role's grant-all seeding in `RbacSeeder`). The frontend License page and its sidebar/route entry SHALL be gated to Super Admin, enforced both by hiding the nav item (`filterMenuByRoles`) and by an in-page `<Navigate>` guard that does not rely on nav visibility alone.

#### Scenario: Admin (non-Super-Admin) cannot view or change license state
- **WHEN** a user with the Admin role (but not Super Admin) navigates directly to `/admin/license` by URL
- **THEN** the page redirects them away via the in-page guard, independent of whether the sidebar link was visible to them

