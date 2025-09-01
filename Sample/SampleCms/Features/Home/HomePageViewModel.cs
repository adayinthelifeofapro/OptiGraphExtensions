namespace SampleCms.Features.Home;

using SampleCms.Features.Common.Pages;

public class HomePageViewModel : ISitePageViewModel<HomePage>
{
    public HomePageViewModel(HomePage currentPage)
    {
        ArgumentNullException.ThrowIfNull(currentPage, nameof(currentPage));
        CurrentPage = currentPage;
    }

    public HomePage CurrentPage { get; set; }
}