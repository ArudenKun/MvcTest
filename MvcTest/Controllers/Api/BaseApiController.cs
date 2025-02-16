using System.Web.Http;

namespace MvcTest.Controllers.Api;

public class BaseApiController : ApiController
{
    [HttpGet]
    public IHttpActionResult Test()
    {
        return Ok();
    }
}
