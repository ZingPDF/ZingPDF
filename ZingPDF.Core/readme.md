# ZingPdf Internal Documentation

## PDF Document Structure

The initial* structure of a PDF should comprise the following objects
- [Header]
- [Body]
- [Cross-reference Table]
- [Trailer]

\* This may be modified by later updates, appended to the end of the file.

### Header
References the version number of the PDF spec to which the file conforms.

Example:
```
%PDF-1.7
```

Note 1: This version number may be overidden in the document's catalog dictionary.

Note 2: If the document is to contain binary data (as most PDFs do) the header should 
be immediately followed by a comment line with at least four binary characters (characters with a code of 128 or greater). 
This ensures proper behaviour of file transfer applications that inspect data near 
the beginning of a file to determine whether to treat the file's contents as text or binary.

Example:
```
%����
```

### Body
Contains a series of objects, such as fonts, pages, images, etc, which represent the PDF document's contents.

### Cross-reference Table
Essentially a table of contents listing all objects and their byte offsets.
This allows random access to all parts of the document.

Initially, this contains a single `xref` section. Each document update may append a new section.

Example: Defines a cross reference section with 5 items, with contiguous numbers starting from 0.
```
xref
0 5
0000000031 65535 f
0000000017 00000 n
0000000165 00000 n
0000000235 00000 n
0000000544 00000 n
```

Note: The first entry must always be a free object and have a generation number of 65535. This entry is the head of the linked list of free entries, as explained [here](#free-entries-linked-list).

Each line must be exactly 20 bytes long, including the EOL marker. The format for each entry is as follows.

```
nnnnnnnnnn ggggg n eol
```

- nnnnnnnnnn - 10 digit byte offset in the decoded stream.
- ggggg - 5 digit generation number, starts at 0. When an object is deleted, this is incremented by 1, and this number is used as the generation number if the object number is reused in a later xref section.
- n - n or f, indicating in-use or free (deleted).
- eol - 2 char end of line sequence.

There are 2 logical mechanisms by which an object number may be a member of the free entries list.

- Linked List: Free entries form a linked list, with the first xref entry being the head. The last free entry links back to object 0;
    - When an object is deleted, its generation number is incremented, and its object number is added to the list, pointing to the head.
- Loose entries: Entries not in the linked list, which are marked as free and point to object 0;

### Trailer

