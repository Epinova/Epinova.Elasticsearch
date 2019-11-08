using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core
{
    internal static class WellKnownProperties
    {
        internal static readonly string[] AutoAnalyze =
        {
            DefaultFields.AttachmentContent,
            "MainIntro",
            "MainBody",
            "Description"
        };

        internal static readonly List<string> Analyze = new List<string>(AutoAnalyze);

        internal static readonly string[] IgnoreAnalyzer =
        {
            DefaultFields.Id,
            DefaultFields.BestBets,
            DefaultFields.ParentLink,
            DefaultFields.Path,
            DefaultFields.Name,
            DefaultFields.Lang,
            DefaultFields.DidYouMean,
            DefaultFields.Suggest,
            DefaultFields.Type,
            DefaultFields.Types,
            DefaultFields.AttachmentData,
        };

        internal static readonly string[] Ignore =
        {
            "CreatedBy",
            "ChangedBy",
            "DeletedBy",
            "URLSegment",
            "ExternalURL",
            "PageName",
            "RouteSegment",
            "LinkURL",
            "StaticLinkURL",
            "LanguageID",
            "LanguageBranch",
            "TargetFrameName",
            "MasterLanguageBranch"
        };
    }
}