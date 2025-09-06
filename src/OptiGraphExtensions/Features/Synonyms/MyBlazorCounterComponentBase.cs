using EPiServer;
using EPiServer.Core;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace OptiGraphExtensions.Features.Synonyms
{
    public class MyBlazorCounterComponentBase : ComponentBase
    {
        [Inject]
        protected IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject]
        protected IContentRepository ContentRepository { get; set; }

        private ContentData _contentData;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (ContentId > 0)
            {
                _contentData = ContentRepository.Get<ContentData>(new ContentReference(ContentId));
            }
        }

        [Parameter]
        public int ContentId { get; set; }

        [Parameter]
        public int Count { get; set; } = 0;

        protected void IncrementCount()
        {
            Count++;
        }

    }
}
