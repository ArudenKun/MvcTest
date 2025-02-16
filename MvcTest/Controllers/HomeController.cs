using System.Globalization;
using System.Web.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MvcTest.Controllers;

public class HomeController : Controller
{
    public ActionResult Index()
    {
        return View();
    }

    public ActionResult About()
    {
        //ViewBag.Message = "Your application description page.";

        //var document = new MismatchDocument();
        //var dataPath = Server.MapPath("~/App_Data");
        //var docPath = Path.Combine(dataPath, $"{Guid.NewGuid()}.pdf");
        //document.GeneratePdf(docPath);

        return File(CreateDocument().GeneratePdf(), "application/pdf");
        //return View();
    }

    public ActionResult Contact()
    {
        ViewBag.Message = "Your contact page.";

        return View();
    }

    public ActionResult Report()
    {
        ViewBag.Message = "Your contact page.";

        return View();
    }

    public ActionResult GeneratePdf()
    {
        byte[] bytes = CreateDocument().GeneratePdf();
        return File(bytes, "application/pdf");
    }

    public ActionResult RenderPdf()
    {
        return PartialView("_ReportView");
    }

    private static IDocument CreateDocument() =>
        Document.Create(container =>
        {
            container.Page(p =>
            {
                p.Size(PageSizes.Legal);
                p.PageColor(Colors.White);
                p.Margin(1, Unit.Centimetre);
                p.Header()
                    .AlignCenter()
                    .Text(
                        """
                        PHILIPPINE NATIONAL BANK
                        TREASURY SERVICES DEPARTMENT
                        COUPON/REDEMPTION PAYMENTS
                        MISMATCH ACCOUNT NAMES
                        """
                    )
                    .Bold();

                var fileName = "Upload Test.xls";
                p.Content()
                    .PaddingTop(1, Unit.Centimetre)
                    .Column(c =>
                    {
                        var now = DateTimeOffset.UtcNow.ToLocalTime();
                        c.Item()
                            .Row(r =>
                            {
                                r.RelativeItem()
                                    .AlignLeft()
                                    .Text($"Original File: {fileName}")
                                    .Bold();
                                r.RelativeItem()
                                    .AlignRight()
                                    .Text($"Date & Time: {now:MM/dd/yyyy}")
                                    .Bold();
                            });
                        c.Item()
                            .PaddingBottom((float)0.5, Unit.Centimetre)
                            .Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text($"{now:h:mm:ss tt}").Bold();
                            });
                        c.Item()
                            .Table(t =>
                            {
                                t.ColumnsDefinition(tc =>
                                {
                                    tc.RelativeColumn();
                                    tc.RelativeColumn();
                                    tc.RelativeColumn();
                                });

                                t.Header(h =>
                                {
                                    h.Cell()
                                        .Element(CellStyle)
                                        .AlignCenter()
                                        .Text("ACCOUNT NUMBER")
                                        .Bold();
                                    h.Cell()
                                        .Element(CellStyle)
                                        .AlignCenter()
                                        .Text(
                                            """
                                            ACCOUNT NAME
                                            CASA ACCOUNT NAME
                                            """
                                        )
                                        .Bold();
                                    h.Cell()
                                        .Element(CellStyle)
                                        .AlignCenter()
                                        .Text("TRANSACTION AMOUNT")
                                        .Bold();
                                });

                                var models = Enumerable
                                    .Range(0, 5)
                                    .Select(i =>
                                        (
                                            Index: i,
                                            TransactionAmount: int.Parse(Placeholders.Integer())
                                        )
                                    )
                                    .ToArray();

                                var pesoCulture = new CultureInfo("en-PH");

                                foreach (var model in models)
                                {
                                    var accountName = Placeholders.Name();
                                    var casaAccountName = Placeholders.Label();

                                    t.Cell()
                                        .Element(CellStyle)
                                        .AlignCenter()
                                        .Text($"{model.Index}");
                                    t.Cell()
                                        .Element(CellStyle)
                                        .AlignCenter()
                                        .Text(
                                            $"""
                                            {accountName}
                                            {casaAccountName}
                                            """
                                        );
                                    t.Cell()
                                        .Element(CellStyle)
                                        .AlignRight()
                                        .Text(
                                            $"{model.TransactionAmount.ToString("C", pesoCulture)}"
                                        );
                                }

                                t.Cell()
                                    .ColumnSpan(2)
                                    .Element(CellStyle)
                                    .AlignRight()
                                    .Text("Grand Total:")
                                    .ExtraBold();
                                t.Cell()
                                    .Element(CellStyle)
                                    .AlignRight()
                                    .Text(
                                        $"{models.Sum(x => x.TransactionAmount).ToString("C", pesoCulture)}"
                                    )
                                    .Bold();
                            });
                        return;

                        static IContainer CellStyle(IContainer container) =>
                            container.Border(1).Padding(10).BorderColor(Colors.Grey.Darken1);
                    });
            });
        });
}
