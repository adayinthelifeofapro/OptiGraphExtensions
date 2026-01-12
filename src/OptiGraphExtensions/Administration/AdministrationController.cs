using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OptiGraphExtensions.Common;

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
            Title = "Opti Graph Extensions - About",
            Subtitle = "OptiGraphExtensions is a comprehensive add-on for Optimizely CMS 12 that provides seamless management of synonyms and pinned results within Optimizely Graph. This add-on enables content editors and administrators to enhance search experiences through intelligent synonym mapping and strategic result pinning capabilities, all integrated directly into your Optimizely CMS administration interface."
        };

        return View("~/Views/OptiGraphExtensions/Administration/About/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/synonyms")]
    public IActionResult Synonyms()
    {
        var model = new AdministrationViewModel
        {
            Title = "Opti Graph Extensions - Synonyms",
        };

        return View("~/Views/OptiGraphExtensions/Administration/Synonyms/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/pinned-results")]
    public IActionResult PinnedResults()
    {
        var model = new AdministrationViewModel
        {
            Title = "Opti Graph Extensions - Pinned Results",
        };

        return View("~/Views/OptiGraphExtensions/Administration/Pinned-Results/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/webhooks")]
    public IActionResult Webhooks()
    {
        var model = new AdministrationViewModel
        {
            Title = "Opti Graph Extensions - Webhooks",
        };

        return View("~/Views/OptiGraphExtensions/Administration/Webhooks/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/query-library")]
    public IActionResult QueryLibrary()
    {
        var model = new AdministrationViewModel
        {
            Title = "Opti Graph Extensions - Query Library",
        };

        return View("~/Views/OptiGraphExtensions/Administration/QueryLibrary/Index.cshtml", model);
    }

    [HttpGet]
    [Route("~/optimizely-graphextensions/administration/request-logs")]
    public IActionResult RequestLogs()
    {
        var model = new AdministrationViewModel
        {
            Title = "Opti Graph Extensions - Request Logs",
        };

        return View("~/Views/OptiGraphExtensions/Administration/RequestLogs/Index.cshtml", model);
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