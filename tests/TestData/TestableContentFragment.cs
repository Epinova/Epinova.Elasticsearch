using System;
using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Security;
using EPiServer.Web;
using Moq;

namespace TestData
{
    public class TestableContentFragment : ContentFragment
    {
        private readonly IContent _content;

        public TestableContentFragment() : this(null)
        {
        }

        public TestableContentFragment(IContent content)
            : base(new Mock<IContentLoader>().Object,
            new Mock<ISecuredFragmentMarkupGenerator>().Object,
            new Mock<DisplayOptions>().Object,
            new Mock<IPublishedStateAssessor>().Object,
            new Mock<IContextModeResolver>().Object,
            new Mock<IContentAccessEvaluator>().Object,
            new Mock<IDictionary<string, object>>().Object)
        {
            _content = content;
            ContentLink = content.ContentLink;
        }

        public override IContent GetContent() => _content;

        public override IContent GetContent(bool enableMasterLanguageFallback) => _content;

        [Obsolete("Use GetContent() instead.", false)]
        public override IContent Content => _content;

        [Obsolete("Use GetContent() instead.", false)]
        public override IContentData ContentData => _content;
    }
}
