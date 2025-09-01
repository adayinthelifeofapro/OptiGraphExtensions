namespace SampleCms.Features.GeneralContent;

using SampleCms.Features.Common.Pages;

public class GeneralContentPageViewModel : ISitePageViewModel<GeneralContentPage>
{
    public GeneralContentPageViewModel(GeneralContentPage currentPage)
    {
        ArgumentNullException.ThrowIfNull(currentPage, nameof(currentPage));

        CurrentPage = currentPage;
    }

    public GeneralContentPage CurrentPage { get; set; }
}