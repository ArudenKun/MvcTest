using System.Web.Mvc;
using MvcTest.Controllers;

namespace MvcTest;

public class FilterConfig
{
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
        filters.Add(new HandleErrorAttribute());
        filters.Add(new NewtonsoftJsonActionFilterAttribute());
    }
}
