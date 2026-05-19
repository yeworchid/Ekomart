using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class ErrorsController : Controller
{
    [Route("/Errors/403")]
    public IActionResult Forbidden()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [Route("/Errors/404")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [Route("/Errors/StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        return code switch
        {
            StatusCodes.Status403Forbidden => Forbidden(),
            StatusCodes.Status404NotFound => NotFoundPage(),
            _ => NotFoundPage()
        };
    }
}
