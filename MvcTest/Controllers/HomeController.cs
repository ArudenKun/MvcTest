using System.Globalization;
using System.Linq.Expressions;
using System.Web.ModelBinding;
using System.Web.Mvc;
using Dapper;
using Dapper.SimpleSqlBuilder;
using FreeSql;
using MvcTest.Models;
using MySqlConnector;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MvcTest.Controllers;

public class HomeController : Controller
{
    private const string ConnectionString = "Server=localhost;Database=cps;User ID=root;Password=;";
    private static IFreeSql _freeSql = new FreeSql.FreeSqlBuilder()
        .UseConnectionString(DataType.MySql, ConnectionString)
        .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
        .Build();

    public ActionResult Index()
    {
        var viewModel = new EmployeeViewModel();
        return View(viewModel);
    }

    [HttpPost]
    public ActionResult UpdateAll()
    {
        var builder = SimpleBuilder
            .CreateFluent()
            .Update($"employee")
            .Set($"is_match = {true}")
            .Where($"is_match = {false}");

        connection.Execute(builder.Sql, builder.Parameters);

        return Json(data: "Approved", behavior: JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public ActionResult Update(int[]? ids)
    {
        if (ids is null || ids.Length == 0)
        {
            return new HttpStatusCodeResult(400, "No valid IDs provided.");
        }

        ids = ids.Distinct().ToArray();

        using var connection = CreateConnection();
        connection.Open();

        var idList = string.Join(", ", ids);
        var builder = SimpleBuilder
            .CreateFluent()
            .Update($"employee")
            .Set($"is_match = {true}")
            .Where($"Id IN ({idList})");

        connection.Execute(builder.Sql, builder.Parameters);
        return Json(data: "Approved", behavior: JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public ActionResult Load([QueryString] bool matched = true)
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

        var builder = SimpleBuilder
            .CreateFluent()
            .Select($"*")
            .From($"employee")
            .Where($"is_match = {matched}");

        if (!string.IsNullOrEmpty(searchValue) && !string.IsNullOrWhiteSpace(searchValue))
        {
            long? parsedId = long.TryParse(searchValue, out var idResult) ? idResult : null;
            DateTimeOffset? parsedBirthDate = DateTimeOffset.TryParse(
                searchValue,
                out var birthDateResult
            )
                ? birthDateResult.UtcDateTime
                : null;

            DateTimeOffset? parsedHireDate = DateTimeOffset.TryParse(
                searchValue,
                out var hireDateResult
            )
                ? hireDateResult.UtcDateTime
                : null;

            builder = builder
                .OrWhere(parsedId.HasValue, $"Id = {parsedId}")
                .OrWhere(
                    parsedBirthDate.HasValue,
                    $"birth_date = {parsedBirthDate!.Value.Date.ToString("MM/dd/yyyy")}"
                )
                .OrWhere(
                    parsedHireDate.HasValue,
                    $"hire_date = {parsedHireDate!.Value.Date.ToString("MM/dd/yyyy")}"
                )
                .OrWhere($"first_name like '%{searchValue}%'")
                .OrWhere($"last_name like '%{searchValue}%'");
        }

        var query = conn.From<Employee>(sql =>
            {
                sql.Where(x => x.IsMatch == matched);
                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue) && !string.IsNullOrWhiteSpace(searchValue))
                {
                    var parsedId = long.TryParse(searchValue, out var idResult) ? idResult : 0;
                    var parsedBirthDate = DateTimeOffset.TryParse(
                        searchValue,
                        out var birthDateResult
                    )
                        ? birthDateResult.UtcDateTime
                        : DateTimeOffset.MinValue.UtcDateTime;
                    ;
                    var parsedHireDate = DateTimeOffset.TryParse(
                        searchValue,
                        out var hireDateResult
                    )
                        ? hireDateResult.UtcDateTime
                        : DateTimeOffset.MinValue.UtcDateTime;

                    sql = sql.Where(e =>
                        e.Id == parsedId
                        || e.FirstName.Contains(searchValue)
                        || e.LastName.Contains(searchValue)
                        || e.BirthDate == parsedBirthDate
                        || e.HireDate == parsedHireDate
                    );
                }

                if (!string.IsNullOrEmpty(order) && !string.IsNullOrEmpty(orderDir))
                {
                    // Apply sorting
                    var columnMap = new Dictionary<string?, Expression<Func<Employee, object?>>>
                    {
                        { "0", e => e.Id },
                        { "1", e => e.BirthDate },
                        { "2", e => e.FirstName },
                        { "3", e => e.LastName },
                        { "4", e => e.HireDate },
                    };

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
                DT_RowId = employee.Id,
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
