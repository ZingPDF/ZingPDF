# Editor library APIs

This example uses public APIs for Unicode font registration, multiline baseline text,
page moves, existing-line replacement with a registered font, and save/reopen validation.
It does not manipulate page trees, content operators or font dictionaries.

From the repository root:

```powershell
dotnet run --project examples/EditorLibraryApi -- tests/TestFiles/font/NotoSans-Regular.ttf artifacts/editor/library-api-example.pdf
```

The font must contain the sample Latin, Greek and Cyrillic characters. The output path
is overwritten. Glyph coverage and embedding are automatic; script shaping and fallback
are outside this example. Existing WinAnsi registration remains available.
