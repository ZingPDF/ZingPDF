# ZingPDF Support And Maintenance Policy

## 1. Scope

This document describes the default support and maintenance policy for paying ZingPDF customers unless a separate enterprise agreement states otherwise.

## 2. Included During Active Support Term

During an active paid support or maintenance term, Licensor may provide:

- access to released updates for covered packages
- bug-fix support for supported versions
- reasonable email or ticket-based technical support
- clarification of documented behavior and supported usage patterns

## 3. Not Included By Default

Unless separately purchased or agreed in writing, support does not include:

- custom feature development
- guaranteed turnaround times
- architecture consulting
- migration or implementation services
- support for modified binaries or unsupported forks
- support for environments outside the published support matrix

## 4. License-Tier Support Levels

Support channels, response expectations, and any service commitments may vary by license type, plan, or negotiated order and may change over time.

Where a checkout page, quote, order, or pricing page defines a specific support level for a plan, that plan-specific description controls.

Solo includes email support. Team and Business include priority email support:
requests are reviewed ahead of Solo requests with comparable business impact.
Production-blocking defects take precedence over routine usage questions within
each support queue. Priority is a handling order, not a guaranteed fix or release date.

Send requests to `tom@zingpdf.dev` with the order email, package version, .NET
version, operating system, and a minimal reproduction. Include a sample document
where needed and lawful to share. For a production-blocking issue, use
`Production issue` in the subject and state the affected workflow, business impact,
and any workaround. Escalate an unresolved request by replying to the same thread
with updated impact and the original request date.

## 5. Supported Versions And Updates

While subscribed, customers may access all generally released versions and updates made available by Licensor.

Licensor may recommend particular versions for supportability, but version access is not limited by plan unless stated in writing.

## 6. Customer Responsibilities

To receive effective support, Customer should provide:

- a clear reproduction of the issue
- sample code or minimal failing input where possible
- environment details, including .NET version, OS, and package version
- logs, exception details, or failing documents where relevant and lawful to share

## 7. Response Targets

Support hours are Monday to Friday, 9am to 5pm in Sydney, Australia
(`Australia/Sydney`, AEST/AEDT as applicable).

| Plan | Initial reply target |
| --- | --- |
| Solo | Within 2 business days |
| Team and Business | Within 1 business day |

We aim to provide an initial human reply within these targets during an active
paid support term. Business days are Monday to Friday in Sydney. For requests
received outside support hours, the target period starts at 9am on the next
weekday.

Any publicly offered response targets are business targets only unless Licensor expressly agrees to an SLA in writing.

An initial reply may request a reproduction, explain a documented limit, or
propose a workaround. It does not guarantee a fix or release date. Guaranteed
response or resolution times and 24/7 incident coverage are not included unless
agreed in writing.

## 8. Maintenance And Updates

An active maintenance term entitles Customer to use updates, patches, and minor releases made generally available during that term, subject to the applicable license scope.

Access to new updates may stop when the maintenance term expires unless renewed.

Expiration of support or maintenance does not end the customer's commercial license to versions released during a paid term. Those versions remain usable within the purchased license scope under `../legal/EULA.md`.

## 9. Supported Usage Boundary

Support is provided for documented and supported product capabilities described in:

- `SUPPORT.md`
- published release notes
- official website/API documentation

Unsupported PDF features, undocumented internals, third-party dependencies, and customer-specific integrations may fall outside normal support scope.

## 10. Suspension Or Refusal Of Support

Licensor may suspend or refuse support where:

- the customer is using the Software outside licensed scope
- fees remain unpaid
- the reported issue concerns a modified or reverse-engineered version of the Software
- the request is abusive, unlawful, or requires services outside the purchased support scope
