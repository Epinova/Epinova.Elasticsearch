using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core
{
    public static class WellKnownProperties
    {
        public static readonly List<string> Analyze = new List<string>(AutoAnalyze);
        public static readonly string[] Highlight = Analyze.ToArray();

        public static readonly string[] AutoAnalyze =
        {
            DefaultFields.AttachmentContent,
            "MainIntro",
            "MainBody",
            "Description"
        };


        public static readonly string[] IgnoreDidYouMean =
        {
            DefaultFields.AttachmentData,
            DefaultFields.Changed,
            DefaultFields.Created,
            DefaultFields.DidYouMean,
            DefaultFields.Id,
            DefaultFields.Indexed,
            DefaultFields.Lang,
            DefaultFields.Path,
            DefaultFields.ParentLink,
            DefaultFields.Suggest,
            DefaultFields.Type,
            DefaultFields.Types
        };

        public static readonly string[] IgnoreAnalyzer =
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

        public static readonly string[] Ignore =
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