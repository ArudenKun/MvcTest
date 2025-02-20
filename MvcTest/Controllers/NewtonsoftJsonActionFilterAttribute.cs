using System.Web.Mvc;
using ActionFilterAttribute = System.Web.Mvc.ActionFilterAttribute;

namespace MvcTest.Controllers;

public class NewtonsoftJsonActionFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        if (filterContext.Result is JsonResult jsonResult)
        {
            filterContext.Result = new NewtonsoftJsonResult()
            {
                ContentEncoding = jsonResult.ContentEncoding,
                ContentType = jsonResult.ContentType,
                Data = jsonResult.Data,
                JsonRequestBehavior = jsonResult.JsonRequestBehavior,
                MaxJsonLength = jsonResult.MaxJsonLength,
                RecursionLimit = jsonResult.RecursionLimit,
            };
        }
    }
}
