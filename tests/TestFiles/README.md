# PDF test fixtures

`pdf/test.pdf` is a synthetic two-page invoice. All names, addresses, identifiers,
dates and amounts are invented. It embeds a Type0 TrueType font with a ToUnicode
map so composite-font extraction remains covered without using customer documents.

`pdf/generated-text-heavy.pdf` repeats that invoice and the image-only fixture to
exercise multi-page parsing, extraction and page editing. Regenerate the workload
fixtures from the `tests` directory:

```powershell
dotnet run --project ../scripts/TestFixtureGenerator/TestFixtureGenerator.csproj --configuration Release
```

Use synthetic data when adding fixtures. Do not use personal bills, customer
records, licence files or production exports as test input.
