using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OptiGraphExtensions.Administration;

[Authorize(Policy = OptiGraphExtensionsConstants.AuthorizationPolicy)]
public sealed class AdministrationController : Controller
{
    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/about")]
    public IActionResult About()
    {
        var model = new AdministrationViewModel
        {
            Title = "Optimizely Graph - About",
            Subtitle = "This is a nested menu example."
        };

        return View("~/Views/OptiGraphExtensions/Administration/About/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/synonyms")]
    public IActionResult Synonyms()
    {
        var model = new AdministrationViewModel
        {
            Title = "Optimizely Graph - Synonyms",
            Subtitle = "This is a nested menu example."
        };

        return View("~/Views/OptiGraphExtensions/Administration/Synonyms/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/pinned-results")]
    public IActionResult PinnedResults()
    {
        var model = new AdministrationViewModel
        {
            Title = "Optimizely Graph - Pinned Results",
            Subtitle = "This is a nested menu example."
        };

        return View("~/Views/OptiGraphExtensions/Administration/Pinned-Results/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-addon/administration/public/list")]
    [AllowAnonymous]
    public IActionResult PublicList()
    {
        var model = new List<string> { "public", "foo", "boo" };

        return Json(model);
    }

    [HttpGet]
    [Route("~/optimizely-addon/administration/private/list")]
    public IActionResult PrivateList()
    {
        var model = new List<string> { "private", "foo", "boo" };

        return Json(model);
    }
}