using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core
{
    public static class WellKnownProperties
    {
        static WellKnownProperties()
        {
            Analyze = new List<string>(AutoAnalyze);
            Highlight = Analyze.ToArray();
        }

        public static readonly List<string> Analyze;

        public static readonly string[] AutoAnalyze =
        {
            DefaultFields.AttachmentContent,
            "MainIntro",
            "MainBody",
            "Description"
        };

        public static readonly string[] ExcludeFromAll =
        {
            DefaultFields.Path
        };


        public static readonly string[] Highlight;


        public static readonly string[] IgnoreDidYouMean =
        {
            DefaultFields.Attachment,
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


        public static readonly string[] Ignore =
        {
            "CreatedBy",
            "ChangedBy",
            "DeletedBy",
            "PageTypeName",
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