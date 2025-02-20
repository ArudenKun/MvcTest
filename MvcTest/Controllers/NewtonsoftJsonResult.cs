using System.Web.Mvc;
using Newtonsoft.Json;

namespace MvcTest.Controllers;

public class NewtonsoftJsonResult : JsonResult
{
    public override void ExecuteResult(ControllerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        var response = context.HttpContext.Response;
        response.ContentType = !string.IsNullOrEmpty(ContentType)
            ? ContentType
            : "application/json";
        if (ContentEncoding != null)
        {
            response.ContentEncoding = ContentEncoding;
        }

        if (Data == null)
            return;
        var json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        response.Write(json);
    }
}
