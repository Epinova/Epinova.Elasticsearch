using System;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Globalization;
using EPiServer.Personalization;
using EPiServer.Shell.Navigation;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions
{
    public abstract class ElasticSearchControllerBase : Controller
    {
        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

            SystemLanguage.Instance.SetCulture();
            UserInterfaceLanguage.Instance.SetCulture(EPiServerProfile.Current == null ? null : EPiServerProfile.Current.Language);
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);

            var platformNavigationMethod = typeof(MenuHelper).GetMethod("CreatePlatformNavigationMenu", new Type[0]);
            if (platformNavigationMethod != null)
            {
                ViewBag.Menu = platformNavigationMethod.Invoke(null, null)?.ToString();
                ViewBag.ContainerClass = "epi-navigation--fullscreen-fixed-adjust";
            }
            else
            {
                ViewBag.Menu = MenuHelper.CreateGlobalMenu(String.Empty, String.Empty);
            }
        }
    }
}