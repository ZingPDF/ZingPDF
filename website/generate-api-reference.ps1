param(
    [string]$ProjectPath = "C:\Users\tom\dev\ZingPDF\ZingPDF\ZingPDF.csproj",
    [string]$XmlPath = "C:\Users\tom\dev\ZingPDF\ZingPDF\bin\Release\net8.0\ZingPDF.xml",
    [string]$OutputPath = "C:\Users\tom\dev\ZingPDF\website\api.html"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$typeConfigs = @(
    @{
        FullName = "ZingPDF.IPdf"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\IPdf.cs"
        Kind = "Interface"
        Group = "Document"
    },
    @{
        FullName = "ZingPDF.PdfMetadata"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\PdfMetadata.cs"
        Kind = "Class"
        Group = "Document"
    },
    @{
        FullName = "ZingPDF.Elements.Page"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Page.cs"
        Kind = "Class"
        Group = "Pages"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.Form"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\Form.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.IFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\IFormField.cs"
        Kind = "Interface"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Text.TextFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Text\TextFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Choice.ChoiceFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Choice\ChoiceFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Choice.ChoiceItem"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Choice\ChoiceItem.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Button.ButtonOptionsFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Button\ButtonOptionsFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Button.CheckboxFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Button\CheckboxFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Button.RadioButtonFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Button\RadioButtonFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Button.PushButtonFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Button\PushButtonFormField.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Button.SelectableOption"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Button\SelectableOption.cs"
        Kind = "Class"
        Group = "Forms"
    },
    @{
        FullName = "ZingPDF.Elements.Forms.FieldTypes.Signature.SignatureFormField"
        FilePath = "C:\Users\tom\dev\ZingPDF\ZingPDF\Elements\Forms\FieldTypes\Signature\SignatureFormField.cs"
        Kind = "Class"
        Group = "Forms"
    }
)

function HtmlEncode([string]$Value) {
    if ($null -eq $Value) {
        return ""
    }

    return [System.Net.WebUtility]::HtmlEncode($Value)
}

function Slugify([string]$Value) {
    return (($Value -replace '[^A-Za-z0-9]+', '-').Trim('-')).ToLowerInvariant()
}

function Get-ShortTypeName([string]$FullName) {
    $normalized = ($FullName -replace '`[0-9]+', '')
    $parts = $normalized.Split('.')
    return $parts[-1]
}

function Convert-CrefToDisplayName([string]$Cref) {
    if ([string]::IsNullOrWhiteSpace($Cref)) {
        return ""
    }

    $trimmed = $Cref
    if ($trimmed.Length -gt 2 -and $trimmed[1] -eq ':') {
        $trimmed = $trimmed.Substring(2)
    }

    if ($trimmed.Contains('(')) {
        $trimmed = $trimmed.Substring(0, $trimmed.IndexOf('('))
    }

    $last = $trimmed.Split('.')[-1]
    return ($last -replace '`[0-9]+', '')
}

function Convert-DocNode([System.Xml.XmlNode]$Node) {
    if ($null -eq $Node) {
        return ""
    }

    switch ($Node.NodeType) {
        "Text" { return (HtmlEncode $Node.InnerText) }
        "Whitespace" { return " " }
        "SignificantWhitespace" { return " " }
        "Element" {
            switch ($Node.Name) {
                "summary" { return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join "" }
                "remarks" { return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join "" }
                "para" { return "<p>$((($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join '').Trim())</p>" }
                "c" { return "<code>$((($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join '').Trim())</code>" }
                "paramref" { return "<code>$(HtmlEncode $Node.Attributes['name'].Value)</code>" }
                "see" {
                    $cref = $null
                    if ($Node.Attributes["cref"]) {
                        $cref = $Node.Attributes["cref"].Value
                    }
                    $label = $Node.InnerText
                    if ([string]::IsNullOrWhiteSpace($label) -and $Node.Attributes["langword"]) {
                        $label = $Node.Attributes["langword"].Value
                    }
                    if ([string]::IsNullOrWhiteSpace($label)) {
                        $label = Convert-CrefToDisplayName $cref
                    }
                    return "<code>$(HtmlEncode $label)</code>"
                }
                "list" {
                    $items = @($Node.SelectNodes("./item"))
                    if ($items.Count -eq 0) {
                        return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join ""
                    }

                    $renderedItems = foreach ($item in $items) {
                        $parts = @()
                        foreach ($child in $item.ChildNodes) {
                            $rendered = (Convert-DocNode $child).Trim()
                            if ($rendered) {
                                $parts += $rendered
                            }
                        }

                        if ($parts.Count -gt 0) {
                            "<li>$($parts -join ' ')</li>"
                        }
                    }

                    return "<ul>$($renderedItems -join '')</ul>"
                }
                "item" { return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join "" }
                "description" { return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join "" }
                "term" { return "<strong>$((($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join '').Trim())</strong>" }
                default { return ($Node.ChildNodes | ForEach-Object { Convert-DocNode $_ }) -join "" }
            }
        }
        default { return "" }
    }
}

function Normalize-DocFragment([string]$Html) {
    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    $normalized = $Html -replace '\s+', ' '
    $normalized = $normalized -replace ' </code>', '</code>'
    $normalized = $normalized -replace '<code> ', '<code>'
    $normalized = $normalized.Trim()

    if ($normalized -notmatch '^<p>' -and $normalized -notmatch '^<ul>' -and $normalized -notmatch '^<ol>') {
        $normalized = "<p>$normalized</p>"
    }

    return $normalized
}

function Get-DocHtml([System.Xml.XmlNode]$Node, [string]$ChildName) {
    $child = $Node.SelectSingleNode($ChildName)
    if ($null -eq $child) {
        return ""
    }

    return Normalize-DocFragment ((Convert-DocNode $child))
}

function Get-MemberKeyFromSignature([string]$Signature) {
    $line = $Signature.Trim().TrimEnd('{').TrimEnd(';').Trim()

    if ($line -match '\b([A-Za-z_][A-Za-z0-9_]*)\s*\(') {
        return $matches[1]
    }

    if ($line -match '([A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|=>|=|$)') {
        return $matches[1]
    }

    return $line
}

function Clean-Signature([string]$Line) {
    $cleaned = ($Line -replace '\basync\s+', '').Trim()
    if ($cleaned.Contains('=>')) {
        $left = $cleaned.Substring(0, $cleaned.IndexOf('=>')).Trim()
        if ($left.Contains('(')) {
            return "$left;"
        }

        return "$left { get; }"
    }

    $cleaned = $cleaned.TrimEnd('{').Trim()
    return $cleaned
}

function Get-TypeSignatureMap([string]$FilePath, [string]$TypeName) {
    $lines = Get-Content -Path $FilePath
    $typePattern = "^\s*public\s+(?:sealed\s+|abstract\s+|partial\s+|static\s+)*?(?:class|interface|record)\s+$([regex]::Escape($TypeName))\b"
    $typeIndex = -1

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $typePattern) {
            $typeIndex = $i
            break
        }
    }

    if ($typeIndex -lt 0) {
        return @{
            TypeSignature = "public type $TypeName"
            Members = @{}
            Order = @()
        }
    }

    $typeSignatureLines = New-Object System.Collections.Generic.List[string]
    for ($i = $typeIndex; $i -lt $lines.Count; $i++) {
        $trimmed = $lines[$i].Trim()
        if (-not [string]::IsNullOrWhiteSpace($trimmed)) {
            $null = $typeSignatureLines.Add($trimmed)
        }

        if ($lines[$i].Contains('{')) {
            break
        }
    }

    $typeSignature = (($typeSignatureLines -join " ") -replace '\s+\{$', '').Trim()
    $isInterface = $typeSignature -match '\binterface\b'
    $braceDepth = 0
    $enteredBody = $false
    $members = @{}
    $order = New-Object System.Collections.Generic.List[string]

    for ($i = $typeIndex; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $trimmed = $line.Trim()

        if (-not $enteredBody -and $line.Contains('{')) {
            $enteredBody = $true
        }
        elseif ($enteredBody -and $braceDepth -eq 1 -and -not [string]::IsNullOrWhiteSpace($trimmed) -and $trimmed -notmatch '^[{}\[\]/]') {
            $looksLikeMember = $false
            if ($isInterface) {
                $looksLikeMember = $trimmed.Contains('(') -or $trimmed.Contains('{ get;')
            }
            else {
                $looksLikeMember = $trimmed -match '^public\s+' -and $trimmed -notmatch '^public\s+(?:sealed\s+|abstract\s+|partial\s+|static\s+)*(?:class|interface|record)\b'
            }

            if ($looksLikeMember) {
            $signature = Clean-Signature $trimmed
            $memberKey = Get-MemberKeyFromSignature $signature

            if ($memberKey -ne $TypeName -and -not $members.ContainsKey($memberKey)) {
                $members[$memberKey] = $signature
                $null = $order.Add($memberKey)
            }
            }
        }

        $openCount = ([regex]::Matches($line, '\{')).Count
        $closeCount = ([regex]::Matches($line, '\}')).Count
        $braceDepth += $openCount - $closeCount

        if ($enteredBody -and $braceDepth -le 0) {
            break
        }
    }

    return @{
        TypeSignature = $typeSignature
        Members = $members
        Order = $order
    }
}

function Get-TypeMemberName([string]$MemberId, [string]$FullTypeName) {
    $body = $MemberId.Substring(2)
    $suffix = $body.Substring($FullTypeName.Length + 1)
    if ($suffix.Contains('(')) {
        $suffix = $suffix.Substring(0, $suffix.IndexOf('('))
    }

    return $suffix
}

function Strip-OuterParagraph([string]$Html) {
    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    if ($Html -match '^<p>(.*)</p>$') {
        return $matches[1]
    }

    return $Html
}

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

dotnet build $ProjectPath -c Release | Out-Host

if (-not (Test-Path $XmlPath)) {
    throw "XML documentation file not found: $XmlPath"
}

[xml]$xml = Get-Content -Path $XmlPath
$memberLookup = @{}
foreach ($member in $xml.doc.members.member) {
    $memberLookup[$member.name] = $member
}

$typePages = foreach ($config in $typeConfigs) {
    $typeMemberId = "T:$($config.FullName)"
    if (-not $memberLookup.ContainsKey($typeMemberId)) {
        continue
    }

    $shortTypeName = Get-ShortTypeName $config.FullName
    $typeDoc = $memberLookup[$typeMemberId]
    $sourceInfo = Get-TypeSignatureMap -FilePath $config.FilePath -TypeName $shortTypeName
    $memberNodes = @()

    foreach ($member in $xml.doc.members.member) {
        if (($member.name.StartsWith("P:$($config.FullName).")) -or ($member.name.StartsWith("M:$($config.FullName)."))) {
            if ($member.name -match '\.#ctor') {
                continue
            }

            $memberNodes += $member
        }
    }

    $memberOrder = @($sourceInfo.Order)
    $orderedMembers = $memberNodes | Sort-Object {
        $name = Get-TypeMemberName -MemberId $_.name -FullTypeName $config.FullName
        $index = $memberOrder.IndexOf($name)
        if ($index -lt 0) { 1000 } else { $index }
    }, {
        Get-TypeMemberName -MemberId $_.name -FullTypeName $config.FullName
    }

    $propertyCards = New-Object System.Collections.Generic.List[string]
    $methodCards = New-Object System.Collections.Generic.List[string]

    foreach ($member in $orderedMembers) {
        $memberName = Get-TypeMemberName -MemberId $member.name -FullTypeName $config.FullName
        $signature = if ($sourceInfo.Members.ContainsKey($memberName)) { $sourceInfo.Members[$memberName] } else { $memberName }
        $summaryHtml = Get-DocHtml -Node $member -ChildName "summary"
        $remarksHtml = Get-DocHtml -Node $member -ChildName "remarks"
        $returnsHtml = Get-DocHtml -Node $member -ChildName "returns"
        $paramItems = @()

        foreach ($param in @($member.SelectNodes("./param"))) {
            $paramBody = Strip-OuterParagraph (Normalize-DocFragment (Convert-DocNode $param))
            $paramItems += "<li><code>$(HtmlEncode $param.name)</code> $paramBody</li>"
        }

        $memberAnchor = Slugify "$shortTypeName-$memberName"
        $memberKind = if ($member.name.StartsWith("P:")) { "Property" } else { "Method" }

        $card = @"
<article class="api-member" id="$memberAnchor">
  <div class="api-member-head">
    <div>
      <p class="eyebrow">$memberKind</p>
      <h3>$memberName</h3>
    </div>
  </div>
  <pre class="api-signature"><code class="language-csharp">$(HtmlEncode $signature)</code></pre>
  $summaryHtml
  $(if ($paramItems.Count -gt 0) { "<div class=""api-meta""><h4>Parameters</h4><ul class=""api-param-list"">$($paramItems -join '')</ul></div>" } else { "" })
  $(if ($returnsHtml) { "<div class=""api-meta""><h4>Returns</h4>$returnsHtml</div>" } else { "" })
  $(if ($remarksHtml) { "<div class=""api-meta""><h4>Remarks</h4>$remarksHtml</div>" } else { "" })
</article>
"@

        if ($memberKind -eq "Property") {
            $null = $propertyCards.Add($card)
        }
        else {
            $null = $methodCards.Add($card)
        }
    }

    [PSCustomObject]@{
        FullName = $config.FullName
        ShortName = $shortTypeName
        Namespace = $config.FullName.Substring(0, $config.FullName.LastIndexOf('.'))
        Kind = $config.Kind
        Group = $config.Group
        Anchor = Slugify $config.FullName
        SummaryHtml = Get-DocHtml -Node $typeDoc -ChildName "summary"
        RemarksHtml = Get-DocHtml -Node $typeDoc -ChildName "remarks"
        TypeSignature = $sourceInfo.TypeSignature
        PropertiesHtml = ($propertyCards -join "`n")
        MethodsHtml = ($methodCards -join "`n")
    }
}

$groups = $typePages | Group-Object Group

$sidebarHtml = foreach ($group in $groups) {
    $links = foreach ($typePage in $group.Group) {
        "<a href=""#$($typePage.Anchor)"">$($typePage.ShortName)</a>"
    }

    @"
<div class="api-nav-group">
  <p class="eyebrow">$($group.Name)</p>
  <nav>
    $($links -join "`n    ")
  </nav>
</div>
"@
}

$typeSections = foreach ($typePage in $typePages) {
    @"
<section class="doc-section api-type" id="$($typePage.Anchor)">
  <div class="api-type-header">
    <div>
      <p class="eyebrow">$($typePage.Kind)</p>
      <h2>$($typePage.ShortName)</h2>
      <p class="api-namespace">$($typePage.Namespace)</p>
    </div>
    <span class="api-type-kind">$($typePage.Kind)</span>
  </div>
  <pre class="api-signature api-type-signature"><code class="language-csharp">$(HtmlEncode $typePage.TypeSignature)</code></pre>
  $($typePage.SummaryHtml)
  $(if ($typePage.RemarksHtml) { "<div class=""api-meta""><h3>Remarks</h3>$($typePage.RemarksHtml)</div>" } else { "" })
  $(if ($typePage.PropertiesHtml) { "<div class=""api-member-group""><h3>Properties</h3>$($typePage.PropertiesHtml)</div>" } else { "" })
  $(if ($typePage.MethodsHtml) { "<div class=""api-member-group""><h3>Methods</h3>$($typePage.MethodsHtml)</div>" } else { "" })
</section>
"@
}

$html = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>ZingPDF API Reference</title>
  <meta name="description" content="Generated API reference for the main ZingPDF public API surface.">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;700&family=Fraunces:opsz,wght@9..144,600;9..144,700&display=swap" rel="stylesheet">
  <link rel="stylesheet" href="./styles.css">
</head>
<body>
  <div class="docs-shell api-page">
    <header class="topbar">
      <a class="brand" href="./index.html">
        <span class="brand-mark">
          <img src="./logo.svg" alt="ZingPDF logo">
        </span>
      </a>
      <nav class="topnav" aria-label="Primary">
        <a href="./index.html#licenses">Licenses</a>
        <a href="./docs.html">Developer Docs</a>
        <a href="./api.html">API Reference</a>
      </nav>
    </header>

    <main>
      <section class="docs-hero api-hero">
        <div class="docs-hero-copy">
          <p class="eyebrow">API Reference</p>
          <h1>Main public API</h1>
        </div>
      </section>

      <div class="docs-layout">
        <aside class="docs-sidebar">
          <h2>Types</h2>
          $($sidebarHtml -join "`n")
        </aside>

        <div class="docs-main">
          <section class="doc-callout api-callout">
            <h3>How to use this page</h3>
            <p>Use the developer guide for workflows and examples, then drop into this reference for canonical signatures, XML-backed member descriptions, and save-time behavior details.</p>
          </section>
          $($typeSections -join "`n")
        </div>
      </div>
    </main>
  </div>
  <script src="./app.js"></script>
</body>
</html>
"@

Set-Content -Path $OutputPath -Value $html -Encoding UTF8
Write-Host "Generated API reference: $OutputPath"
