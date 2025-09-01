using EPiServer.Shell.Navigation;

namespace OptiGraphExtensions.Menus;

[MenuProvider]
public sealed class OptiGraphExtensionsMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        // Nested Menu Example
        yield return CreateMenuItem("Graph Extensions", "/global/cms/optigraphextensions", "/optimizely-graphextensions/administration/about/", SortIndex.Last + 30);
        yield return CreateMenuItem("Synonyms", "/global/cms/optigraphextensions/synonyms", "/optimizely-graphextensions/administration/synonyms", SortIndex.Last + 32);
        yield return CreateMenuItem("Pinned Results", "/global/cms/optigraphextensions/pinned.results", "/optimizely-graphextensions/administration/pinned-results", SortIndex.Last + 33);
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
