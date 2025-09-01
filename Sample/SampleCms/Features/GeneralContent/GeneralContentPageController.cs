namespace SampleCms.Features.GeneralContent;

using Microsoft.AspNetCore.Mvc;

using SampleCms.Features.Common;

public class GeneralContentPageController : PageControllerBase<GeneralContentPage>
{
    public IActionResult Index(GeneralContentPage currentContent)
    {
        var model = new GeneralContentPageViewModel(currentContent);

        return View(model);
    }
}
