using System;
using System.Collections.Generic;
using System.Web.Routing;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.Shell.Navigation;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    [MenuProvider]
    public class ElasticSearchMenuProvider : IMenuProvider
    {
        private static Func<RequestContext, bool> GetAccessInfo()
            => request => PrincipalInfo.CurrentPrincipal.IsInRole(RoleNames.ElasticsearchAdmins);

        private readonly Func<string, string> _translate = key => LocalizationService.Current.GetString(String.Concat("/epinovaelasticsearch/", key));

        public IEnumerable<MenuItem> GetMenuItems()
        {
            var main = new SectionMenuItem(_translate("heading"), "/global/epinovaelasticsearchmenu")
            {
                IsAvailable = GetAccessInfo()
            };

            var admin = new UrlMenuItem(_translate("admin/heading"), "/global/epinovaelasticsearchmenu/admin", "/ElasticSearchAdmin/ElasticAdmin")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 0
            };

            var tracking = new UrlMenuItem(_translate("tracking/heading"), "/global/epinovaelasticsearchmenu/tracking", "/ElasticSearchAdmin/ElasticTracking")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 10
            };

            var index = new UrlMenuItem(_translate("indexinspector/heading"), "/global/epinovaelasticsearchmenu/index", "/ElasticSearchAdmin/ElasticIndexInspector")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 40
            };

            var synonyms = new UrlMenuItem(_translate("synonyms/heading"), "/global/epinovaelasticsearchmenu/synonyms", "/ElasticSearchAdmin/ElasticSynonyms")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 20
            };

            var bestbets = new UrlMenuItem(_translate("bestbets/heading"), "/global/epinovaelasticsearchmenu/synonyms", "/ElasticSearchAdmin/ElasticBestBets")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 25
            };

            var boosting = new UrlMenuItem(_translate("boosting/heading"), "/global/epinovaelasticsearchmenu/boosting", "/ElasticSearchAdmin/ElasticBoosting")
            {
                IsAvailable = x => false //TODO Broken, must be reviewed
            };

            var autosuggest = new UrlMenuItem(_translate("autosuggest/heading"), "/global/epinovaelasticsearchmenu/autosuggest", "/ElasticSearchAdmin/ElasticAutoSuggest")
            {
                IsAvailable = x => false, //TODO Revise usefullness
                SortIndex = 30
            };

            var console = new UrlMenuItem(_translate("console/heading"), "/global/epinovaelasticsearchmenu/console", "/ElasticSearchAdmin/ElasticConsole")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 50
            };

            var mapping = new UrlMenuItem(_translate("console/mapping"), "/global/epinovaelasticsearchmenu/mapping", "/ElasticSearchAdmin/ElasticConsole/Mapping")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 60
            };

            var settings = new UrlMenuItem(_translate("console/settings"), "/global/epinovaelasticsearchmenu/settings", "/ElasticSearchAdmin/ElasticConsole/Settings")
            {
                IsAvailable = GetAccessInfo(),
                SortIndex = 70
            };

            return new MenuItem[]
            {
                main, admin, tracking, index, synonyms, bestbets, boosting, autosuggest, console, mapping, settings
            };
        }
    }
}