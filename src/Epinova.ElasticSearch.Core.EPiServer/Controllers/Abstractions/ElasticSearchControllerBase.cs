using System;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Globalization;
using EPiServer.Personalization;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions
{
    public abstract class ElasticSearchControllerBase : Controller
    {
        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            
            SystemLanguage.Instance.SetCulture();
            UserInterfaceLanguage.Instance.SetCulture(EPiServerProfile.Current == null ? null: EPiServerProfile.Current.Language);
        }

        protected string CurrentLanguage
        {
            get => TempData[nameof(CurrentLanguage)] as string ?? String.Empty;
            set => TempData[nameof(CurrentLanguage)] = value;
        }
    }
}