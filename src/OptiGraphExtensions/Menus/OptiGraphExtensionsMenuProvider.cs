using EPiServer.Shell.Navigation;

using OptiGraphExtensions.Common;

namespace OptiGraphExtensions.Menus;

[MenuProvider]
public sealed class OptiGraphExtensionsMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        // Nested Menu Example
        yield return CreateMenuItem("Opti Graph Extensions", "/global/cms/optigraphextensions", "/optimizely-graphextensions/administration/about/", SortIndex.Last + 30);
        yield return CreateMenuItem("About", "/global/cms/optigraphextensions/about", "/optimizely-graphextensions/administration/about/", SortIndex.Last + 31);
        yield return CreateMenuItem("Synonyms", "/global/cms/optigraphextensions/synonyms", "/optimizely-graphextensions/administration/synonyms/", SortIndex.Last + 32);
        yield return CreateMenuItem("Pinned Results", "/global/cms/optigraphextensions/pinned.results", "/optimizely-graphextensions/administration/pinned-results/", SortIndex.Last + 33);
        yield return CreateMenuItem("Webhooks", "/global/cms/optigraphextensions/webhooks", "/optimizely-graphextensions/administration/webhooks/", SortIndex.Last + 34);
        yield return CreateMenuItem("Query Library", "/global/cms/optigraphextensions/querylibrary", "/optimizely-graphextensions/administration/query-library/", SortIndex.Last + 35);
        yield return CreateMenuItem("Request Logs", "/global/cms/optigraphextensions/requestlogs", "/optimizely-graphextensions/administration/request-logs/", SortIndex.Last + 36);
        yield return CreateMenuItem("Custom Data", "/global/cms/optigraphextensions/customdata", "/optimizely-graphextensions/administration/custom-data/", SortIndex.Last + 37);
    }

    private static UrlMenuItem CreateMenuItem(string name, string path, string url, int index)
    {
        return new UrlMenuItem(name, path, url)
        {
            IsAvailable = context => true,
            SortIndex = index,
            AuthorizationPolicy = OptiGraphExtensionsConstants.AuthorizationPolicy
        };
    }
}
