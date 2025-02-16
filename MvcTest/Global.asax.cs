using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using QuestPDF.Infrastructure;

namespace MvcTest;

public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        QuestPDF.Settings.License = LicenseType.Professional;
        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }
}
