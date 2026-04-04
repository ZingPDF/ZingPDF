# ZingPDF Website Style Guide

## Purpose

This site sells a technical product to technical readers.

The audience is usually:

- .NET developers evaluating a PDF library
- engineers trying to solve a specific PDF problem
- technical buyers checking capability, pricing, and constraints

They will notice vague, padded, or synthetic copy quickly.

The goal of the site is not to sound polished. The goal is to be clear, specific, and credible.

## Core Rule

Write about:

- what the library does
- when to use it
- what the API looks like
- what the document format is doing
- what the constraints or gaps are

Do not write about:

- the content strategy
- the audience from a distance
- how the site is maintained
- what the page is trying to do
- what marketing teams or buyers usually think

If a sentence is about the site rather than the product, it probably should not be there.

## Voice

Use:

- plainspoken technical English
- short declarative sentences
- concrete nouns and verbs
- direct references to PDF behavior, API behavior, runtime constraints, and licensing facts

Prefer:

- "Read and update AcroForm fields by name."
- "Call `RemoveHistoryAsync()` before saving if you need a rewritten file."
- "The output stream must be writable and seekable."

Avoid:

- "practical"
- "straightforward"
- "robust"
- "powerful"
- "flexible"
- "helpful"
- "seamless"
- "strong"
- "focused"

Those words usually weaken the copy unless they are backed by a measured fact.

## Write For A Technical Reader

Assume the reader wants to know:

1. What does this do?
2. What are the inputs and outputs?
3. What is the catch?
4. Will this work on my files?
5. What is not implemented yet?

Good copy answers those questions directly.

Bad copy describes the reader instead of answering the question.

## Banned Patterns

Do not use owner-facing or process-facing lines like:

- "These guides are just static pages..."
- "without introducing a blog engine or CMS"
- "what buyers usually ask first"
- "the tasks people actually search for"
- "trying to get a feature working"
- "the parts that usually trip people up"
- "the docs go deeper on the edge cases"

Do not use audience-observer phrasing like:

- "teams care about"
- "what teams evaluate first"
- "people actually search for"
- "buyers usually ask"

Do not use filler framing like:

- "the useful part is"
- "worth knowing"
- "what this means is"
- "the nice thing is"
- "the point is"

## Specificity Rules

Prefer this:

- "Fast page access and plain-text extraction when your code opens lots of existing PDFs."
- "Supports RC4-128, AES-128, and AES-256 output."
- "Push-button action execution is not exposed through the high-level API."

Not this:

- "Strong performance where teams feel it."
- "Broad document coverage."
- "Practical examples."
- "Useful benchmark coverage."

If you can replace an abstract phrase with a PDF operation, API name, benchmark number, or runtime fact, do that.

## Page-Type Guidance

### Homepage

The homepage should explain:

- what ZingPDF is
- what it already covers
- where it is fast
- how pricing works
- where the current gaps are documented

Do not turn the homepage into startup copywriting.

### Guides And Articles

Articles should read like technical how-to pieces.

They should:

- start from the problem
- show the code early
- explain the PDF behavior that changes the result
- call out real limits plainly

They should not:

- explain that they are "implementation guides"
- narrate the SEO intent
- describe the article as useful, practical, or code-first

### Docs

Docs should sound like reference material.

They can say:

- what types exist
- what methods do
- what stream requirements exist
- what is and is not implemented

They should not drift into positioning copy.

### Performance

Performance copy must stay tied to measurements.

Say:

- what benchmark was run
- what number ZingPDF produced
- how it compared

Do not say:

- "strong performance"
- "highly competitive"
- "excellent results"

Use numbers instead.

### Pricing

Pricing copy should be factual.

Say:

- seat counts
- support level
- billing model
- how to handle procurement or invoicing

Do not describe plans with marketing adjectives.

### Legal Pages

Legal pages are not marketing copy.

Do not edit them for tone unless there is a legal reason and the meaning is preserved.

## Review Checklist

Before publishing copy, check:

1. Does each section name real PDF operations, API calls, constraints, or pricing facts?
2. Does any sentence describe the audience instead of the product?
3. Does any sentence explain the site, the content strategy, or the maintenance model?
4. Can any adjective be removed without losing meaning?
5. If a benchmark or capability claim is made, is it measured or explicit?
6. Would a technical reader say this naturally?

If the answer to 2 or 3 is yes, rewrite it.

## Enforcement

### Manual

Before merging copy changes:

1. Read this file.
2. Run `pwsh ./website/check-copy.ps1`.
3. Read the changed copy aloud once.
4. Rewrite any sentence that sounds like it was written about the reader rather than for the reader.

### When Using AI

Treat AI output as a draft only.

Always do a human pass that removes:

- filler adjectives
- audience-observer phrasing
- meta commentary about the page
- generic SaaS language

### CI Or Pre-Deploy

At minimum, run the copy check script before deploy.

If you want stricter enforcement later, add `pwsh ./website/check-copy.ps1` to the website deploy workflow so flagged phrases fail the build.

