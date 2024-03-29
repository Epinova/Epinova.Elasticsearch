﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using Epinova.ElasticSearch.Core.EPiServer.Extensions;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Editor;
    using EPiServer.Security;
    using EPiServer.Shell.Web.Mvc.Html;
    using EPiServer.SpecializedProperties;
    using EPiServer.Web;
    using EPiServer.Web.Mvc.Html;
    using EPiServer.Web.Routing;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/ElasticSearchAdmin/Console/Index.cshtml")]
    public partial class _Views_ElasticSearchAdmin_Console_Index_cshtml : System.Web.Mvc.WebViewPage<Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.ConsoleViewModel>
    {
        public _Views_ElasticSearchAdmin_Console_Index_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

  
    Layout = "~/Views/ElasticSearchAdmin/_ElasticSearch.cshtml";

WriteLiteral("\r\n\r\n");

DefineSection("Styles", () => {

WriteLiteral("\r\n    <style>\r\n        .Sleek .dijitTextArea {\r\n            width: 99%;\r\n        " +
"    min-width: 99%;\r\n            max-width: 99%;\r\n        }\r\n\r\n        .Sleek .d" +
"ijitReadOnly {\r\n            opacity: .8;\r\n        }\r\n    </style>\r\n");

});

WriteLiteral("\r\n");

  
    string localizationPath = "/epinovaelasticsearch/console/";

WriteLiteral("\r\n\r\n<div");

WriteLiteral(" class=\"epi-padding-small\"");

WriteLiteral(">\r\n");

    
     using (Html.BeginForm("Index", "ElasticConsole"))
    {

WriteLiteral("        <h2>");

       Write(Html.TranslateWithPath("query", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("        <label>\r\n");

WriteLiteral("            ");

       Write(Html.TranslateWithPathRaw("index", localizationPath));

WriteLiteral("\r\n            <select");

WriteLiteral(" data-dojo-type=\"dijit/form/Select\"");

WriteLiteral(" name=\"index\"");

WriteLiteral(">\r\n");

                
                 foreach (string index in Model.Indices)
                {

WriteLiteral("                    <option");

WriteAttribute("value", Tuple.Create(" value=\"", 1058), Tuple.Create("\"", 1082)
, Tuple.Create(Tuple.Create("", 1066), Tuple.Create<System.Object, System.Int32>(Html.Raw(index)
, 1066), false)
);

WriteAttribute("selected", Tuple.Create(" selected=\"", 1083), Tuple.Create("\"", 1186)
, Tuple.Create(Tuple.Create("", 1094), Tuple.Create<System.Object, System.Int32>(Model.SelectedIndex.Equals(index, StringComparison.OrdinalIgnoreCase) ? "selected" : null
, 1094), false)
);

WriteLiteral(">");

                                                                                                                                                        Write(index);

WriteLiteral("</option>\r\n");

                }

WriteLiteral("            </select>\r\n        </label>\r\n");

WriteLiteral("        <textarea");

WriteLiteral(" name=\"query\"");

WriteLiteral(" data-dojo-type=\"dijit/form/SimpleTextarea\"");

WriteLiteral(" data-dojo-props=\"style:\'height:100px;\'\"");

WriteLiteral(">");

                                                                                                             Write(Model.Query);

WriteLiteral("</textarea>\r\n");

WriteLiteral("        <p>\r\n            <button");

WriteLiteral(" data-dojo-type=\"dijit/form/Button\"");

WriteLiteral(" type=\"submit\"");

WriteLiteral(" class=\"epi-primary\"");

WriteLiteral(">");

                                                                                    Write(Html.TranslateWithPathRaw("execute", localizationPath));

WriteLiteral("</button>\r\n        </p>\r\n");

    }

WriteLiteral("\r\n");

    
     if (!string.IsNullOrWhiteSpace(Model.Result))
    {

WriteLiteral("        <h2>");

       Write(Html.TranslateWithPath("result", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("        <pre>");

        Write(Model.Result);

WriteLiteral(")</pre>\r\n");

    }

WriteLiteral("</div>\r\n");

        }
    }
}
#pragma warning restore 1591
