using System.Globalization;
using System.Linq.Expressions;
using System.Web.ModelBinding;
using System.Web.Mvc;
using FreeSql;
using MvcTest.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZiggyCreatures.Caching.Fusion;

namespace MvcTest.Controllers;

public class HomeController : Controller
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=;Database=employees;Pooling=true;Minimum Pool Size=1";

    private static readonly IFreeSql FreeSql = new FreeSqlBuilder()
        .UseConnectionString(DataType.PostgreSQL, ConnectionString)
        .UseMonitorCommand(cmd => Console.WriteLine($"Sql: {cmd.CommandText}"))
        .Build();

    private static readonly IFusionCache FusionCache = new FusionCache(new FusionCacheOptions());

    public ActionResult Index()
    {
        var viewModel = new EmployeeViewModel();
        return View(viewModel);
    }

    [HttpPost]
    public ActionResult ApproveAll()
    {
        FreeSql
            .Update<Employee>()
            .Set(x => x.IsMatch, true)
            .Where(x => x.IsMatch == false)
            .ExecuteAffrows();

        FusionCache.Remove($"{nameof(Load)}-rows-total");
        FusionCache.RemoveByTag([nameof(Employee), nameof(Load)]);

        return Json(data: "Approved", behavior: JsonRequestBehavior.AllowGet);
    }

    [HttpPost]
    public ActionResult Approve(long[]? ids)
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

        FusionCache.Remove($"{nameof(Load)}-rows-total");
        FusionCache.RemoveByTag([nameof(Employee), nameof(Load)]);
        return Json(data: "Approved");
    }

    public ActionResult Disapprove(long[]? ids)
    {
        if (ids is null || ids.Length == 0)
        {
            return new HttpStatusCodeResult(400, "No valid IDs provided.");
        }

        ids = ids.Distinct().ToArray();

        FreeSql.Delete<Employee>().Where(x => ids.Contains(x.Id)).ExecuteAffrows();
        FusionCache.Remove($"{nameof(Load)}-rows-total");
        FusionCache.RemoveByTag([nameof(Employee), nameof(Load)]);

        return Json(data: "Disapproved");
    }

    [HttpPost]
    public ActionResult Load([QueryString] bool matched = true)
    {
        var draw = int.TryParse(
            Request.Form.GetValues("draw")?.FirstOrDefault()?.Trim(),
            out var intParseResult
        )
            ? intParseResult
            : 0;
        var start = Request.Form.GetValues("start")?.FirstOrDefault()?.Trim();
        var length = Request.Form.GetValues("length")?.FirstOrDefault()?.Trim();
        var order =
            Request
                .Form.GetValues(
                    "columns["
                        + Request.Form.GetValues("order[0][column]")?.FirstOrDefault()
                        + "][data]"
                )
                ?.FirstOrDefault()
                ?.Trim() ?? string.Empty;
        var orderDir =
            Request.Form.GetValues("order[0][dir]")?.FirstOrDefault()?.Trim() ?? string.Empty;
        var search =
            Request.Form.GetValues("search[value]")?.FirstOrDefault()?.Trim().ToLower()
            ?? string.Empty;

        var pageSize = length != null ? Convert.ToInt32(length) : 10; // Default pageSize if null
        var skip = start != null ? Convert.ToInt32(start) : 0;
        var page = pageSize > 0 ? skip / pageSize + 1 : 1;

        var cacheKey =
            $"matched:{matched}_page:{page}_pageSize:{pageSize}_search:{search}_order:{order}_orderDir:{orderDir}";

        // Build the base query
        var query = FreeSql.Select<Employee>().Where(x => x.IsMatch == matched);

        // Apply filtering
        if (!string.IsNullOrWhiteSpace(search))
        {
            long? parsedId = long.TryParse(search, out var idResult) ? idResult : null;
            DateTime? parsedDate = DateTime.TryParse(search, out var dateResult)
                ? dateResult
                : null;
            Expression<Func<Employee, bool>> where = null!;
            if (parsedId.HasValue)
                where = where.Or(x => x.Id == parsedId);
            if (parsedDate.HasValue)
                where = where.Or(x => x.BirthDate == parsedDate || x.HireDate == parsedDate);
            query = query.Where(where);
        }

        // Sorting
        query = query.OrderByPropertyNameIf(
            !string.IsNullOrEmpty(order) && !string.IsNullOrEmpty(orderDir),
            order,
            orderDir.Equals("ASC", StringComparison.CurrentCultureIgnoreCase)
        );

        // Get total records before filtering
        var rowsTotalCache = FusionCache.GetOrSet(
            $"{nameof(Load)}-rows-total",
            _ => FreeSql.Select<Employee>().Count(),
            options => options.SetDurationMin(10)
        );

        // Get total records after filtering
        var rowsFilteredCache = FusionCache.GetOrSet(
            $"{cacheKey}-rows-filtered",
            _ => query.Count(),
            options => options.SetDurationMin(10),
            tags: [nameof(Employee), nameof(Load)]
        );

        // Get records
        var rowsCache = FusionCache.GetOrSet(
            $"{cacheKey}-rows",
            _ =>
                query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList(employee => new
                    {
                        employee.Id,
                        employee.FirstName,
                        employee.LastName,
                        employee.BirthDate,
                        employee.HireDate,
                    }),
            options => options.SetDurationMin(10),
            tags: [nameof(Employee), nameof(Load)]
        );

        var result = DataTableDto.Create(
            draw,
            rowsTotalCache,
            rowsFilteredCache,
            rowsCache.Select(employee => new LoadDto
            {
                RowId = employee.Id,
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                BirthDate = employee.BirthDate.ToLocalTime().ToString("MM/dd/yyyy"),
                HireDate = employee.HireDate.ToLocalTime().ToString("MM/dd/yyyy"),
            })
        );

        return Json(result);
    }

    public ActionResult Upload()
    {
        return new EmptyResult();
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
