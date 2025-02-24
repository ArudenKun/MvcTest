using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MvcTest.Models;
using QuestPDF.Infrastructure;

namespace MvcTest;

public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        QuestPDF.Settings.License = LicenseType.Professional;
        ModelBinders.Binders.Add(typeof(DataTablesRequest), new DataTablesRequestBinder());
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }
}
