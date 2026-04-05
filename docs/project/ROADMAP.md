# Roadmap

## Backlog

### Sanitization API

Practical document sanitization looks like a real library use case, especially for reusing form templates and removing sensitive payloads before redistribution.

Potential public API areas to explore:

- `pdf.RemoveMetadataAsync(...)`
- `page.ClearContentAsync()` or `pdf.RemovePageContentAsync(...)`
- `form.ClearValuesAsync(...)`
- `form.RegenerateAppearancesAsync(...)`
- `pdf.RemoveImagesAsync(...)` with scoped options
- `pdf.RemoveAnnotationsAsync(...)` with filtering
- `pdf.RemoveAttachmentsAsync()`
- field rename or randomization helpers
- a higher-level `pdf.SanitizeAsync(SanitizationOptions)` orchestration API

Important design notes:

- Keep the first version composable rather than one giant monolithic method.
- Make sanitization scope explicit: form data, page content, annotations, metadata, attachments, history.
- Split image removal by category where needed, since page artwork, button icons, stamp appearances, and attachments are not the same thing.
- Document the difference between convenience cleanup, structural cleanup, and stronger history-removal workflows.
