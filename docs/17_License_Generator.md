# 17 — License Generator

`backend/tools/LitXus.LicenseGenerator` is the offline, vendor-side tool that mints signed license keys. It's the counterpart to `ILicenseKeyVerifier` on the deployed app side (see [16_Feature_Flags.md](16_Feature_Flags.md) §16.4).

## 17.1 Trust boundary

- **Private key** — stays only on the machine that runs this tool. Never committed, never shipped with a deployment. Already gitignored (`*.pem`, `backend/tools/LitXus.LicenseGenerator/keys/`).
- **Public key** — the only half that goes into a deployment's `appsettings.json` → `Licensing:PublicKeyPem`. It can verify signatures, not produce them.

This tool has no dependency on `LitXus.Application`/`Infrastructure` — it's a separate, offline process, not part of the product binary.

## 17.2 Generate a keypair (once per vendor identity)

```bash
cd backend
dotnet run --project tools/LitXus.LicenseGenerator -- generate-keypair --out-dir tools/LitXus.LicenseGenerator/keys
```

Writes `license-private.pem` (keep offline) and `license-public.pem` (goes into `appsettings.json`, see §17.4). Run once — every deployment trusting keys from this vendor identity needs the matching public key in its config.

## 17.3 Generate a license (per customer/token)

```bash
dotnet run --project tools/LitXus.LicenseGenerator -- generate-license \
  --private-key tools/LitXus.LicenseGenerator/keys/license-private.pem \
  --product AccountingPro \
  --company "Acme Sdn Bhd" \
  --modules Accounting,Sales \
  --expires 2027-12-31
```

| Flag | Notes |
|---|---|
| `--private-key` | Path to `license-private.pem` from §17.2 |
| `--product` | Free-text, e.g. `AccountingPro`, `RetailPro`, `EnterprisePro` (see [00_Overview.md](00_Overview.md)) |
| `--company` | Customer name; quote if it has spaces |
| `--modules` | Comma-separated, from the set `Accounting`, `Sales`, `Inventory` only |
| `--expires` | Any `yyyy-MM-dd` date |

Prints a signed JWT (RS256) to stdout — that's **the license key**. Nothing else is printed, so it's safe to pipe (`> license.txt`) or paste directly.

## 17.4 Applying a key

The JWT from §17.3 — never a `.pem` file — is what gets pasted into the app. Log in as Super Admin, go to **Administration → License**, paste the token into "Apply New License Key", and submit. On success the backend replaces `ProductCode`/`IssuedToCompany`/`EnabledModules`/`ExpiresAtUtc` from the token's claims and the new module set applies immediately, no restart needed.

For a brand-new deployment, its `appsettings.json` → `Licensing:PublicKeyPem` must already hold the public half of whatever keypair will sign that customer's keys — set this before first boot.

## 17.5 Rotating keys

Generating a new keypair doesn't invalidate keys already applied to a deployment's database. But a key signed with a *new* private key won't verify against a deployment still configured with the *old* public key — update `Licensing:PublicKeyPem` there first. Rotation is a deliberate, infrequent operation (e.g. suspected key compromise), not part of normal issuance.

## 17.6 Troubleshooting

| Rejection message | Likely cause |
|---|---|
| "the signature is invalid or the key is malformed" | Wrong keypair (signed with a private key whose public half isn't the one configured), pasted a `.pem` file instead of the JWT, or the paste got truncated |
| "this key has expired" | `--expires` is in the past relative to the server's clock |
| "missing productCode/company claim" | Valid JWT, but not one produced by this tool |



------
Step 1:
cd backend
dotnet run --project tools/LitXus.LicenseGenerator -- generate-keypair --out-dir tools/LitXus.LicenseGenerator/keys

note: No need to redo Step 1 — the keypair is generated once per vendor identity and reused indefinitely. To change which modules a license grants


Step 2:
dotnet run --project tools/LitXus.LicenseGenerator -- generate-license \
  --private-key tools/LitXus.LicenseGenerator/keys/license-private.pem \
  --product AccountingPro \
  --company "Acme Sdn Bhd" \
  --modules Accounting,Sales \
  --expires 2027-12-31

  note: -expires 2099-12-31     for longer years
