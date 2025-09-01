namespace SampleCms.Features.Home;

using Microsoft.AspNetCore.Mvc;

using SampleCms.Features.Common;

public class HomePageController : PageControllerBase<HomePage>
{
    public IActionResult Index(HomePage currentPage)
    {
        var model = new HomePageViewModel(currentPage);

        return View(model);
    }
}
