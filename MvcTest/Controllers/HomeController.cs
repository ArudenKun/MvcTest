using System.Globalization;
using System.Linq.Expressions;
using System.Web.Mvc;
using Dommel;
using MvcTest.Models;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MvcTest.Controllers;

public class HomeController : Controller
{
    private const string ConnectionString =
        "Username=postgres;Database=employees;Password=\" \";Host=localhost";

    private NpgsqlConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public ActionResult Index()
    {
        using var conn = CreateConnection();
        conn.Open();

        var employees = conn.From<Employee>(sql => sql.Page(1, 100).Select());
        var viewModel = new EmployeeViewModel { Employees = employees };
        return View(viewModel);
    }

    public ActionResult Load()
    {
        var draw = Request.Form.GetValues("draw")?.FirstOrDefault();
        var start = Request.Form.GetValues("start")?.FirstOrDefault();
        var length = Request.Form.GetValues("length")?.FirstOrDefault();
        var order = Request.Form.GetValues("order[0][column]")?.FirstOrDefault();
        var orderDir = Request.Form.GetValues("order[0][dir]")?.FirstOrDefault();
        var searchValue = Request.Form.GetValues("search[value]")?.FirstOrDefault()?.ToLower();

        var pageSize = length != null ? Convert.ToInt32(length) : 0;
        var skip = start != null ? Convert.ToInt32(start) : 0;

        using var conn = CreateConnection();

        var query = conn.From<Employee>(sql =>
            {
                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue) && !string.IsNullOrWhiteSpace(searchValue))
                {
                    var parsedId = long.TryParse(searchValue, out var result) ? result : 0;
                    var parsedBirthDate = DateTimeOffset.TryParse(searchValue, out var dateResult)
                        ? dateResult.UtcDateTime
                        : DateTimeOffset.UtcNow;

                    var parsedHireDate = DateTimeOffset.TryParse(
                        searchValue,
                        out var hireDateResult
                    )
                        ? hireDateResult.UtcDateTime
                        : DateTimeOffset.UtcNow;
                    sql = sql.Where(e =>
                        (
                            e.Id == parsedId
                            || e.BirthDate == parsedBirthDate
                            || e.FirstName.Contains(searchValue)
                            || e.LastName.Contains(searchValue)
                            || e.HireDate == parsedHireDate
                        )
                    );
                }

                // Apply sorting
                var columnMap = new Dictionary<string?, Expression<Func<Employee, object?>>>
                {
                    { "0", e => e.Id },
                    { "1", e => e.BirthDate },
                    { "2", e => e.FirstName },
                    { "3", e => e.LastName },
                    { "4", e => e.HireDate },
                };

                if (!string.IsNullOrEmpty(order) && !string.IsNullOrEmpty(orderDir))
                {
                    if (columnMap.TryGetValue(order, out var sortExpression))
                    {
                        if (!string.IsNullOrEmpty(orderDir))
                        {
                            sql =
                                orderDir is not null
                                && orderDir.Equals(
                                    "DESC",
                                    StringComparison.CurrentCultureIgnoreCase
                                )
                                    ? sql.OrderByDescending(sortExpression)
                                    : sql.OrderBy(sortExpression);
                        }
                    }
                    else
                    {
                        sql = sql.OrderBy(e => e.Id);
                    }
                }

                sql.Select();
            })
            .ToArray();

        // Get total records count
        var recordsTotal = conn.Count<Employee>();
        var recordsFiltered = query.Length;
        // Apply pagination
        var records = query
            .Skip(skip)
            .Take(pageSize)
            .Select(employee => new
            {
                employee.Id,
                employee.FirstName,
                employee.LastName,
                BirthDate = employee.BirthDate.ToLocalTime().ToString("MM/dd/yyyy"),
                HireDate = employee.HireDate.ToLocalTime().ToString("MM/dd/yyyy"),
            });

        return Json(
            new
            {
                draw = Convert.ToInt32(draw),
                recordsFiltered,
                recordsTotal,
                data = records,
            }
        );
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
        var bytes = CreateDocument().GeneratePdf();
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
