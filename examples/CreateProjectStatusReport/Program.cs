using ZingPDF;
using ZingPDF.Graphics;

Directory.CreateDirectory("output");

var outputPath = Path.Combine("output", "project-status-report.pdf");

await Pdf.New()
    .Page(BuildCoverPage)
    .Page(BuildDeliveryPage)
    .SaveToFileAsync(outputPath);

Console.WriteLine($"Wrote {outputPath}");

static void BuildCoverPage(PdfAuthoringBuilder.PdfPageAuthoringBuilder page)
{
    page.Size(595, 842);

    AddBackground(page, 0, 0, 595, 842, Palette.Paper);
    AddBackground(page, 0, 712, 595, 130, Palette.Navy);
    AddBackground(page, 0, 688, 595, 24, Palette.Teal);

    page.Text(text => text
        .Value("Northwind ERP Modernisation")
        .HelveticaBold()
        .FontSize(24)
        .Color(Palette.White)
        .At(48, 780));

    page.Text(text => text
        .Value("Monthly delivery report")
        .Helvetica()
        .FontSize(14)
        .Color(Palette.Sky)
        .At(48, 754));

    page.Text(text => text
        .Value("Reporting period")
        .HelveticaBold()
        .FontSize(10)
        .Color(Palette.Sky)
        .At(48, 720));

    page.Text(text => text
        .Value("March 2026")
        .HelveticaBold()
        .FontSize(10.5)
        .Color(Palette.Navy)
        .InBox(48, 692, 120, 16)
        .AlignStart()
        .AlignMiddle()
        .Padding(0));

    AddInfoChip(page, 412, 740, 135, 58, "Prepared for", "Northwind Retail Group");
    AddInfoChip(page, 412, 670, 135, 58, "Prepared by", "Delivery Office");

    AddSectionHeading(page, "Delivery snapshot", 48, 646);
    AddMetricCard(page, 48, 554, 148, 74, "Overall status", "On track", Palette.SuccessSoft, Palette.SuccessText);
    AddMetricCard(page, 223, 554, 148, 74, "Milestones", "14 / 16", Palette.InfoSoft, Palette.InfoText);
    AddMetricCard(page, 398, 554, 149, 74, "Budget used", "61%", Palette.WarningSoft, Palette.WarningText);

    AddSectionHeading(page, "Completed this month", 48, 512);
    AddCard(page, 48, 330, 240, 162, Palette.White, Palette.Border);
    AddBullet(page, 68, 458, "Finance and procurement workflows released");
    AddBullet(page, 68, 424, "Cutover rehearsal completed in staging");
    AddBullet(page, 68, 390, "Support playbooks handed to operations");
    AddBullet(page, 68, 356, "Role-based access review signed off");

    AddSectionHeading(page, "Upcoming milestones", 307, 512);
    AddCard(page, 307, 330, 240, 162, Palette.White, Palette.Border);
    AddMilestoneRow(page, 327, 458, "15 Apr", "Warehouse pilot go-live", "Planned");
    AddMilestoneRow(page, 327, 424, "29 Apr", "Finance close on new platform", "Planned");
    AddMilestoneRow(page, 327, 390, "06 May", "Executive readiness review", "Booked");
    AddMilestoneRow(page, 327, 356, "20 May", "National rollout decision", "Pending");

    AddSectionHeading(page, "Executive summary", 48, 290);
    AddCard(page, 48, 128, 499, 136, Palette.White, Palette.Border);
    AddBodyLine(page, 68, 228, "Delivery remains on track for the phased May rollout.");
    AddBodyLine(page, 68, 204, "The largest remaining dependency is supplier onboarding for the warehouse pilot.");
    AddBodyLine(page, 68, 180, "No material scope changes were approved during the reporting period.");
    AddBodyLine(page, 68, 156, "The team recommends staying with the current deployment window.");

    AddFooter(page, "Northwind ERP Modernisation", 1);
}

static void BuildDeliveryPage(PdfAuthoringBuilder.PdfPageAuthoringBuilder page)
{
    page.Size(595, 842);

    AddBackground(page, 0, 0, 595, 842, Palette.Paper);
    AddBackground(page, 0, 760, 595, 82, Palette.Navy);

    page.Text(text => text
        .Value("Operational detail")
        .HelveticaBold()
        .FontSize(24)
        .Color(Palette.White)
        .At(48, 792));

    page.Text(text => text
        .Value("Delivery risks, next steps, and implementation decisions")
        .Helvetica()
        .FontSize(12)
        .Color(Palette.Sky)
        .At(48, 770));

    AddSectionHeading(page, "Risk register", 48, 724);
    AddCard(page, 48, 456, 320, 238, Palette.White, Palette.Border);
    AddRiskHeader(page, 68, 666);
    AddRiskRow(page, 68, 634, "R-14", "Warehouse supplier data quality", "Medium", "PMO");
    AddRiskRow(page, 68, 586, "R-19", "Finance user training attendance", "Medium", "Training");
    AddRiskRow(page, 68, 538, "R-22", "Cutover checklist sign-off timing", "Low", "Ops");
    AddRiskRow(page, 68, 490, "R-27", "Legacy integration timeout spikes", "Low", "Platform");

    AddSectionHeading(page, "Next 30 days", 360, 724);
    AddCard(page, 380, 456, 187, 238, Palette.White, Palette.Border);
    AddNumberedItem(page, 398, 638, 1, "Run the warehouse pilot readiness review.");
    AddNumberedItem(page, 398, 592, 2, "Lock migration payloads for finance close.");
    AddNumberedItem(page, 398, 546, 3, "Complete support handover with service desk.");
    AddNumberedItem(page, 398, 500, 4, "Confirm rollout communications with business leads.");

    AddSectionHeading(page, "Architecture decisions", 48, 418);
    AddCard(page, 48, 166, 499, 224, Palette.White, Palette.Border);
    AddDecision(page, 68, 356, "Adopt queue-based inventory sync to isolate branch outages.");
    AddDecision(page, 68, 318, "Keep reporting workloads on the existing analytics store until phase two.");
    AddDecision(page, 68, 280, "Use signed PDF delivery packs for rollout approvals and audit trails.");
    AddDecision(page, 68, 242, "Preserve incremental history only for internal review copies.");

    AddSectionHeading(page, "Approvals and contacts", 48, 136);
    AddCard(page, 48, 60, 499, 64, Palette.InfoSoft, Palette.Border);
    AddContactColumn(page, 68, 72, 130, "Programme Director", "Alicia Chen");
    AddContactColumn(page, 228, 72, 130, "Delivery Lead", "Marcus Patel");
    AddContactColumn(page, 388, 72, 139, "PMO Contact", "delivery-office@northwind.example", 9);

    AddFooter(page, "Northwind ERP Modernisation", 2);
}

static void AddBackground(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, double width, double height, RGBColour fill)
{
    page.Rectangle(box => box
        .At(x, y)
        .Size(width, height)
        .WithoutStroke()
        .Fill(fill));
}

static void AddCard(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, double width, double height, RGBColour fill, RGBColour border)
{
    page.Rectangle(box => box
        .At(x, y)
        .Size(width, height)
        .Stroke(border, 1)
        .Fill(fill));
}

static void AddSectionHeading(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, string title, double x, double y)
{
    page.Text(text => text
        .Value(title)
        .HelveticaBold()
        .FontSize(13)
        .Color(Palette.Ink)
        .At(x, y));
}

static void AddBodyLine(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, string textValue)
{
    page.Text(text => text
        .Value(textValue)
        .Helvetica()
        .FontSize(10.5)
        .Color(Palette.Muted)
        .At(x, y));
}

static void AddInfoChip(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, double width, double height, string label, string value)
{
    AddCard(page, x, y, width, height, new RGBColour(0.12, 0.18, 0.31), new RGBColour(0.24, 0.35, 0.56));

    page.Text(text => text
        .Value(label)
        .HelveticaBold()
        .FontSize(9)
        .Color(Palette.Sky)
        .At(x + 14, y + 34));

    page.Text(text => text
        .Value(value)
        .HelveticaBold()
        .FontSize(11)
        .Color(Palette.White)
        .InBox(x + 14, y + 12, width - 28, 18)
        .AlignStart()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(8));
}

static void AddMetricCard(
    PdfAuthoringBuilder.PdfPageAuthoringBuilder page,
    double x,
    double y,
    double width,
    double height,
    string label,
    string value,
    RGBColour fill,
    RGBColour textColour)
{
    AddCard(page, x, y, width, height, fill, Palette.Border);

    page.Text(text => text
        .Value(label)
        .HelveticaBold()
        .FontSize(10)
        .Color(Palette.Muted)
        .At(x + 18, y + 46));

    page.Text(text => text
        .Value(value)
        .HelveticaBold()
        .FontSize(22)
        .Color(textColour)
        .At(x + 18, y + 16));
}

static void AddBullet(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, string value)
{
    page.Rectangle(box => box
        .At(x, y + 5)
        .Size(8, 8)
        .WithoutStroke()
        .Fill(Palette.Teal));

    page.Text(text => text
        .Value(value)
        .Helvetica()
        .FontSize(10)
        .Color(Palette.Muted)
        .InBox(x + 18, y - 2, 190, 18)
        .AlignStart()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(8.5));
}

static void AddMilestoneRow(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, string date, string item, string state)
{
    page.Line(line => line
        .From(x, y - 8)
        .To(x + 204, y - 8)
        .Stroke(Palette.Border, 1));

    page.Text(text => text
        .Value(date)
        .HelveticaBold()
        .FontSize(10)
        .Color(Palette.Ink)
        .At(x, y));

    page.Text(text => text
        .Value(item)
        .Helvetica()
        .FontSize(10)
        .Color(Palette.Muted)
        .InBox(x + 52, y - 2, 110, 18)
        .AlignStart()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(8));

    page.Text(text => text
        .Value(state)
        .HelveticaBold()
        .FontSize(9)
        .Color(Palette.TealDark)
        .InBox(x + 166, y - 4, 38, 18)
        .AlignCenter()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(6));
}

static void AddRiskHeader(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y)
{
    page.Text(text => text.Value("ID").HelveticaBold().FontSize(9).Color(Palette.Muted).At(x, y));
    page.Text(text => text.Value("Issue").HelveticaBold().FontSize(9).Color(Palette.Muted).At(x + 42, y));
    page.Text(text => text.Value("Impact").HelveticaBold().FontSize(9).Color(Palette.Muted).At(x + 200, y));
    page.Text(text => text.Value("Owner").HelveticaBold().FontSize(9).Color(Palette.Muted).At(x + 248, y));
    page.Line(line => line.From(x, y - 8).To(x + 286, y - 8).Stroke(Palette.Border, 1));
}

static void AddRiskRow(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, string id, string issue, string impact, string owner)
{
    page.Text(text => text.Value(id).HelveticaBold().FontSize(10).Color(Palette.Ink).InBox(x, y - 12, 34, 18).AlignStart().AlignMiddle().Padding(0));
    page.Text(text => text.Value(issue).Helvetica().FontSize(9.25).Color(Palette.Muted).InBox(x + 42, y - 12, 150, 18).AlignStart().AlignMiddle().Padding(0).ShrinkToFit(7.75));
    page.Text(text => text.Value(impact).Helvetica().FontSize(9.25).Color(Palette.Muted).InBox(x + 200, y - 12, 40, 18).AlignStart().AlignMiddle().Padding(0).ShrinkToFit(7.5));
    page.Text(text => text.Value(owner).Helvetica().FontSize(9.25).Color(Palette.Muted).InBox(x + 248, y - 12, 40, 18).AlignStart().AlignMiddle().Padding(0).ShrinkToFit(7));
    page.Line(line => line.From(x, y - 18).To(x + 286, y - 18).Stroke(Palette.Border, 1));
}

static void AddNumberedItem(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, int number, string value)
{
    page.Rectangle(box => box
        .At(x, y + 2)
        .Size(18, 18)
        .WithoutStroke()
        .Fill(Palette.Navy));

    page.Text(text => text
        .Value(number.ToString())
        .HelveticaBold()
        .FontSize(10)
        .Color(Palette.White)
        .InBox(x, y + 2, 18, 18)
        .AlignCenter()
        .AlignMiddle()
        .Padding(0));

    page.Text(text => text
        .Value(value)
        .Helvetica()
        .FontSize(9.5)
        .Color(Palette.Muted)
        .InBox(x + 28, y - 18, 121, 38)
        .AlignStart()
        .AlignTop()
        .Padding(0)
        .Wrap()
        .ClipOverflow());
}

static void AddDecision(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, string value)
{
    page.Line(line => line
        .From(x, y - 10)
        .To(x + 452, y - 10)
        .Stroke(Palette.Border, 1));

    page.Text(text => text
        .Value("•")
        .HelveticaBold()
        .FontSize(18)
        .Color(Palette.TealDark)
        .At(x, y - 2));

    page.Text(text => text
        .Value(value)
        .Helvetica()
        .FontSize(10.5)
        .Color(Palette.Muted)
        .At(x + 18, y));
}

static void AddFooter(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, string documentName, int pageNumber)
{
    page.Line(line => line
        .From(48, 36)
        .To(547, 36)
        .Stroke(Palette.Border, 1));

    page.Text(text => text
        .Value(documentName)
        .Helvetica()
        .FontSize(9)
        .Color(Palette.Muted)
        .At(48, 20));

    page.Text(text => text
        .Value($"Page {pageNumber}")
        .Helvetica()
        .FontSize(9)
        .Color(Palette.Muted)
        .At(510, 20));
}

static void AddContactColumn(PdfAuthoringBuilder.PdfPageAuthoringBuilder page, double x, double y, double width, string label, string value, double valueFontSize = 10)
{
    page.Text(text => text
        .Value(label)
        .HelveticaBold()
        .FontSize(10)
        .Color(Palette.Ink)
        .InBox(x, y + 13, width, 14)
        .AlignStart()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(8.5));

    page.Text(text => text
        .Value(value)
        .Helvetica()
        .FontSize(valueFontSize)
        .Color(Palette.Muted)
        .InBox(x, y - 4, width, 16)
        .AlignStart()
        .AlignMiddle()
        .Padding(0)
        .ShrinkToFit(7));
}

static class Palette
{
    public static RGBColour Paper => new(0.96, 0.97, 0.99);
    public static RGBColour White => RGBColour.White;
    public static RGBColour Navy => new(0.08, 0.11, 0.20);
    public static RGBColour Teal => new(0.12, 0.66, 0.70);
    public static RGBColour TealDark => new(0.07, 0.45, 0.48);
    public static RGBColour Sky => new(0.79, 0.88, 0.96);
    public static RGBColour Ink => new(0.11, 0.15, 0.23);
    public static RGBColour Muted => new(0.34, 0.40, 0.52);
    public static RGBColour Border => new(0.83, 0.87, 0.92);
    public static RGBColour SuccessSoft => new(0.90, 0.97, 0.93);
    public static RGBColour SuccessText => new(0.15, 0.44, 0.27);
    public static RGBColour InfoSoft => new(0.91, 0.96, 1.00);
    public static RGBColour InfoText => new(0.12, 0.33, 0.58);
    public static RGBColour WarningSoft => new(1.00, 0.96, 0.89);
    public static RGBColour WarningText => new(0.56, 0.37, 0.08);
}
