# Report preview

`project-status-report.pdf` is generated from the repository's
`examples/CreateProjectStatusReport` source using the published ZingPDF NuGet
package. Regenerate it with both downloadable homepage examples:

```powershell
pwsh ./scripts/update-homepage-examples.ps1
```

The PNG is the first page of the two-page PDF. Regenerate both assets together
when changing the example. The report contains fictional demonstration data.
See `../examples/README.md` for independent verification and visual review.
