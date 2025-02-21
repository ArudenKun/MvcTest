using System.Globalization;
using System.Linq.Expressions;
using System.Web.ModelBinding;
using System.Web.Mvc;
using FreeSql;
using MvcTest.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MvcTest.Controllers;

public class HomeController : Controller
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=;Database=employees;Pooling=true;Minimum Pool Size=1";

    private static readonly IFreeSql FreeSql = new FreeSqlBuilder()
        .UseConnectionString(DataType.PostgreSQL, ConnectionString)
        .UseMonitorCommand(cmd => Console.WriteLine($"Sql: {cmd.CommandText}"))
        .Build();

    public ActionResult Index()
    {
        var viewModel = new EmployeeViewModel();
        return View(viewModel);
    }

    [HttpPost]
    public ActionResult UpdateAll()
    {
        FreeSql
            .Update<Employee>()
            .Set(x => x.IsMatch, true)
            .Where(x => x.IsMatch == false)
            .ExecuteAffrows();

        return Json(data: "Approved", behavior: JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public ActionResult Update(long[]? ids)
    {
        if (ids is null || ids.Length == 0)
        {
            return new HttpStatusCodeResult(400, "No valid IDs provided.");
        }

        ids = ids.Distinct().ToArray();

        FreeSql
            .Update<Employee>()
            .Set(x => x.IsMatch, true)
            .Where(x => ids.Contains(x.Id))
            .ExecuteAffrows();

        return Json(data: "Approved", behavior: JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public ActionResult Load([QueryString] bool matched = true)
    {
        var draw = Request.Form.GetValues("draw")?.FirstOrDefault();
        var start = Request.Form.GetValues("start")?.FirstOrDefault();
        var length = Request.Form.GetValues("length")?.FirstOrDefault();
        var order = Request.Form.GetValues("order[0][column]")?.FirstOrDefault() ?? string.Empty;
        var orderDir = Request.Form.GetValues("order[0][dir]")?.FirstOrDefault() ?? string.Empty;
        var searchValue =
            Request.Form.GetValues("search[value]")?.FirstOrDefault()?.ToLower() ?? string.Empty;

        var pageSize = length != null ? Convert.ToInt32(length) : 10; // Default pageSize if null
        var skip = start != null ? Convert.ToInt32(start) : 0;
        var page = pageSize > 0 ? skip / pageSize + 1 : 1;

        // Build the base query
        var query = FreeSql.Select<Employee>().Where(x => x.IsMatch == matched);

        // Apply filtering
        if (!string.IsNullOrWhiteSpace(searchValue))
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

            Expression<Func<Employee, bool>> where = null!;

            where = where.Or(x => x.FirstName.Contains(searchValue));
            where = where.Or(x => x.LastName.Contains(searchValue));

            if (parsedId.HasValue)
            {
                where = where.Or(x => x.Id == parsedId);
            }

            if (parsedBirthDate.HasValue)
            {
                where = where.Or(x => x.BirthDate == parsedBirthDate);
            }

            if (parsedHireDate.HasValue)
            {
                where = where.Or(x => x.HireDate == parsedHireDate);
            }

            query = query.Where(where);
        }

        // Sorting Map
        var columnMap = new Dictionary<string?, Expression<Func<Employee, object?>>>
        {
            { "0", e => e.Id },
            { "1", e => e.FirstName },
            { "2", e => e.LastName },
            { "3", e => e.BirthDate },
            { "4", e => e.HireDate },
        };

        if (columnMap.TryGetValue(order, out var sortExpression))
        {
            query = query.OrderByIf(
                !string.IsNullOrEmpty(order) && !string.IsNullOrEmpty(orderDir),
                sortExpression,
                orderDir.Equals("DESC", StringComparison.CurrentCultureIgnoreCase)
            );
        }
        else
        {
            query = query.OrderBy(e => e.Id); // Default ordering
        }

        // Get total records before filtering
        var rowsTotal = FreeSql.Select<Employee>().Count();
        // Get total records after filtering
        var rowsFiltered = query.Count();
        // Fetch paginated data (avoid unnecessary loading of full records)
        var rows = query
            .Page(page, pageSize)
            .ToList()
            .Select(employee => new
            {
                DT_RowId = employee.Id,
                employee.Id,
                employee.FirstName,
                employee.LastName,
                BirthDate = employee.BirthDate.ToLocalTime().ToString("MM/dd/yyyy"),
                HireDate = employee.HireDate.ToLocalTime().ToString("MM/dd/yyyy"),
            })
            .ToArray();

        return Json(
            new
            {
                draw = Convert.ToInt32(draw),
                recordsFiltered = rowsFiltered,
                recordsTotal = rowsTotal,
                data = rows,
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
