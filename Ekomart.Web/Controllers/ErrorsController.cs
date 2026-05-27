using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class ErrorsController : Controller
{
    [Route("/Errors/403")]
    public IActionResult Forbidden()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["ErrorTitle"] = "Access Is Forbidden";
        ViewData["ErrorMessage"] = "You do not have permission to open this page. Please return to homepage.";
        return View("NotFound");
    }

    [Route("/Errors/401")]
    public IActionResult UnauthorizedPage()
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        ViewData["ErrorTitle"] = "Login Is Required";
        ViewData["ErrorMessage"] = "Please sign in to continue or return to homepage.";
        return View("NotFound");
    }

    [Route("/Errors/404")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["ErrorTitle"] = "This Page Can't Be Found";
        ViewData["ErrorMessage"] = "Sorry, we couldn't find the page you were looking for. We suggest that you return to homepage.";
        return View("NotFound");
    }

    [Route("/Errors/500")]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["ErrorTitle"] = "Something Went Wrong";
        ViewData["ErrorMessage"] = "The page cannot be opened right now. Please return to homepage and try again later.";
        return View("NotFound");
    }

    [Route("/Errors/StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        return code switch
        {
            StatusCodes.Status401Unauthorized => UnauthorizedPage(),
            StatusCodes.Status403Forbidden => Forbidden(),
            StatusCodes.Status404NotFound => NotFoundPage(),
            StatusCodes.Status500InternalServerError => ServerError(),
            _ => ServerError()
        };
    }
}
