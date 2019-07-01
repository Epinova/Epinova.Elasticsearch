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
    using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/ElasticSearchAdmin/BestBets/Index.cshtml")]
    public partial class _Views_ElasticSearchAdmin_BestBets_Index_cshtml : System.Web.Mvc.WebViewPage<BestBetsViewModel>
    {
        public _Views_ElasticSearchAdmin_BestBets_Index_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

WriteLiteral("\r\n");

  
    ViewBag.DojoParseOnLoad = false;
    Layout = "~/Views/ElasticSearchAdmin/_ElasticSearch.cshtml";
    string localizationPath = "/epinovaelasticsearch/bestbets/";

WriteLiteral("\r\n\r\n");

 if (Model == null)
{
    return;
}

WriteLiteral("\r\n");

DefineSection("Styles", () => {

WriteLiteral(@"
    <style>
        #loader {
            display: block;
            margin: 25% auto;
        }

        #tabContainer .field-phrase {
            width: 180px;
        }

        #tabContainer .field-contentId {
            width: 180px;
        }

        #tabContainer .field-actions {
            width: 120px;
            text-align: center;
        }

        #tabContainer .dgrid-cell a {
            color: #187dcf !important;
            cursor: pointer !important;
            text-decoration: underline !important;
        }

        .pageSelector .dijitInputField {
            min-height: 16px;
        }

        .Sleek .epi-form-container__section__row {
            padding-left: 0 !important;
        }

            .Sleek .epi-form-container__section__row > label {
                width: 70px !important;
            }
    </style>
");

});

WriteLiteral("\r\n<img");

WriteLiteral(" id=\"loader\"");

WriteLiteral(" alt=\"Loading...\"");

WriteAttribute("src", Tuple.Create(" src=\"", 1415), Tuple.Create("\"", 1540)
, Tuple.Create(Tuple.Create("", 1421), Tuple.Create<System.Object, System.Int32>(EPiServer.Shell.Paths.ToShellClientResource("ClientResources/epi/themes/sleek/epi/images/icons/ajaxProgress-salt.gif")
, 1421), false)
);

WriteLiteral(" />\r\n\r\n<div");

WriteLiteral(" id=\"tabContainer\"");

WriteLiteral(" style=\"visibility: hidden;\"");

WriteLiteral(">\r\n    <div");

WriteLiteral(" data-dojo-type=\"dijit/layout/TabContainer\"");

WriteLiteral(" doLayout=\"false\"");

WriteLiteral(">\r\n");

        
         foreach (BestBetsByLanguage bb in Model.BestBetsByLanguage)
        {

WriteLiteral("            <div");

WriteLiteral(" data-dojo-type=\"dijit/layout/ContentPane\"");

WriteAttribute("title", Tuple.Create(" title=\"", 1811), Tuple.Create("\"", 1835)
, Tuple.Create(Tuple.Create("", 1819), Tuple.Create<System.Object, System.Int32>(bb.LanguageName
, 1819), false)
);

WriteLiteral(" data-dojo-props=\"");

                                                                                                 Write(bb.LanguageId == Model.CurrentLanguage ? "selected:true" : null);

WriteLiteral("\"");

WriteLiteral(">\r\n                <div");

WriteLiteral(" class=\"epi-padding-small\"");

WriteLiteral(">\r\n");

                    
                     if (ViewBag.Indices != null)
                    {
                        using (Html.BeginForm("Index", "ElasticBestBets"))
                        {

WriteLiteral("                            <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"LanguageId\"");

WriteAttribute("value", Tuple.Create(" value=\"", 2216), Tuple.Create("\"", 2238)
, Tuple.Create(Tuple.Create("", 2224), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 2224), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                            <h2>");

                           Write(Html.TranslateWithPathRaw("index", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("                            <p>\r\n                                <label>\r\n       " +
"                             <select");

WriteLiteral(" data-dojo-type=\"dijit/form/Select\"");

WriteLiteral(" name=\"index\"");

WriteLiteral(">\r\n");

                                        
                                         foreach (string index in ViewBag.Indices)
                                        {

WriteLiteral("                                            <option");

WriteAttribute("value", Tuple.Create(" value=\"", 2682), Tuple.Create("\"", 2696)
, Tuple.Create(Tuple.Create("", 2690), Tuple.Create<System.Object, System.Int32>(index
, 2690), false)
);

WriteAttribute("selected", Tuple.Create(" selected=\"", 2697), Tuple.Create("\"", 2761)
, Tuple.Create(Tuple.Create("", 2708), Tuple.Create<System.Object, System.Int32>(ViewBag.SelectedIndex == index ? "selected" : null
, 2708), false)
);

WriteLiteral(">");

                                                                                                                               Write(index);

WriteLiteral("</option>\r\n");

                                        }

WriteLiteral("                                    </select>\r\n                                </" +
"label>\r\n                                <button");

WriteLiteral(" data-dojo-type=\"dijit/form/Button\"");

WriteLiteral(" type=\"submit\"");

WriteLiteral(" class=\"epi-primary\"");

WriteLiteral(">");

                                                                                                        Write(Html.TranslateWithPathRaw("show", localizationPath));

WriteLiteral("</button>\r\n                            </p>\r\n");

                        }
                    }

WriteLiteral("\r\n");

                    
                     using (Html.BeginForm("Add", "ElasticBestBets"))
                    {

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"LanguageId\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3326), Tuple.Create("\"", 3348)
, Tuple.Create(Tuple.Create("", 3334), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3334), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"Index\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3411), Tuple.Create("\"", 3441)
, Tuple.Create(Tuple.Create("", 3419), Tuple.Create<System.Object, System.Int32>(ViewBag.SelectedIndex
, 3419), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"TypeName\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3507), Tuple.Create("\"", 3532)
, Tuple.Create(Tuple.Create("", 3515), Tuple.Create<System.Object, System.Int32>(ViewBag.TypeName
, 3515), false)
);

WriteLiteral(" />\r\n");



WriteLiteral("                        <h2>");

                       Write(Html.TranslateWithPath("newbestbet", localizationPath));

WriteLiteral("</h2>\r\n");



WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-form-container__section__row epi-form-container__section__row--field\"" +
"");

WriteLiteral(">\r\n                            <label");

WriteAttribute("for", Tuple.Create(" for=\"", 3778), Tuple.Create("\"", 3805)
, Tuple.Create(Tuple.Create("", 3784), Tuple.Create("phrase_", 3784), true)
, Tuple.Create(Tuple.Create("", 3791), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3791), false)
);

WriteLiteral(">");

                                                          Write(Html.TranslateWithPathRaw("phrase", localizationPath));

WriteLiteral("</label>\r\n                            <input");

WriteAttribute("id", Tuple.Create(" id=\"", 3905), Tuple.Create("\"", 3931)
, Tuple.Create(Tuple.Create("", 3910), Tuple.Create("phrase_", 3910), true)
, Tuple.Create(Tuple.Create("", 3917), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3917), false)
);

WriteLiteral(" data-dojo-type=\"dijit/form/ValidationTextBox\"");

WriteLiteral(" name=\"phrase\"");

WriteLiteral(" data-dojo-props=\"required:true\"");

WriteLiteral(" />\r\n                        </div>\r\n");

WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-form-container__section__row epi-form-container__section__row--field\"" +
"");

WriteLiteral(">\r\n                            <label");

WriteAttribute("for", Tuple.Create(" for=\"", 4207), Tuple.Create("\"", 4240)
, Tuple.Create(Tuple.Create("", 4213), Tuple.Create("pageSelector_", 4213), true)
, Tuple.Create(Tuple.Create("", 4226), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 4226), false)
);

WriteLiteral(">");

                                                                Write(Html.TranslateWithPathRaw("contentid", localizationPath));

WriteLiteral("</label>\r\n                            <div");

WriteAttribute("id", Tuple.Create(" id=\"", 4341), Tuple.Create("\"", 4373)
, Tuple.Create(Tuple.Create("", 4346), Tuple.Create("pageSelector_", 4346), true)
, Tuple.Create(Tuple.Create("", 4359), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 4359), false)
);

WriteLiteral("\r\n                                 class=\"pageSelector\"");

WriteLiteral("\r\n                                 data-dojo-type=\"epi-cms/widget/ContentSelector" +
"\"");

WriteLiteral("\r\n                                 data-dojo-props=\"roots: [\'");

                                                      Write(String.Join("','", Model.SelectorRoots));

WriteLiteral("\'], repositoryKey: \'pages\', allowedTypes: [\'");

                                                                                                                                          Write(String.Join("','", Model.SelectorTypes));

WriteLiteral("\'], allowedDndTypes: [], value: null, required:true\"");

WriteLiteral("></div>\r\n                            <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"contentId\"");

WriteAttribute("id", Tuple.Create(" id=\"", 4822), Tuple.Create("\"", 4851)
, Tuple.Create(Tuple.Create("", 4827), Tuple.Create("contentId_", 4827), true)
, Tuple.Create(Tuple.Create("", 4837), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 4837), false)
);

WriteLiteral(" />\r\n                        </div>\r\n");

WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-form-container__section__row epi-form-container__section__row--field\"" +
"");

WriteLiteral(">\r\n                            <button");

WriteLiteral(" data-dojo-type=\"dijit/form/Button\"");

WriteLiteral(" type=\"submit\"");

WriteLiteral(" class=\"epi-primary\"");

WriteLiteral(">");

                                                                                                    Write(Html.TranslateWithPath("addnew", localizationPath));

WriteLiteral("</button>\r\n                        </div>\r\n");



WriteLiteral("                        <h2>");

                       Write(Html.TranslateWithPath("existingbestbets", localizationPath));

WriteLiteral("</h2>\r\n");



WriteLiteral("                        <div");

WriteAttribute("id", Tuple.Create(" id=\"", 5328), Tuple.Create("\"", 5362)
, Tuple.Create(Tuple.Create("", 5333), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 5333), false)
, Tuple.Create(Tuple.Create("", 5349), Tuple.Create("-bestbetsGrid", 5349), true)
);

WriteLiteral("></div>\r\n");

                    }

WriteLiteral("                </div>\r\n            </div>\r\n");

        }

WriteLiteral(@"    </div>
</div>

<script>
    function loadScript() {
        require([
            ""dojo/_base/declare"",
            ""dojo/_base/connect"",
            ""dojo/query"",
            ""dojo/when"",
            ""dijit/registry"",
            ""dgrid/Grid"",
            ""dijit/form/Button"",
            ""dijit/_Widget"",
            ""dijit/_TemplatedMixin"",
            ""epi/epi"",
            ""epi/dependency"",
            ""epi/shell/widget/dialog/Confirmation"",
            ""epi-cms/core/PermanentLinkHelper"",
            ""dojo/domReady!""
        ], function (
            declare,
            connect,
            query,
            when,
            registry,
            Grid,
            Button,
            _Widget,
            _TemplatedMixin,
            epi,
            dependency,
            Confirmation,
            PermanentLinkHelper
        ) {
");

            
             foreach (BestBetsByLanguage bestBetByLanguage in Model.BestBetsByLanguage)
            {
                string lang = bestBetByLanguage.LanguageId;

WriteLiteral("                ");

WriteLiteral("\r\n                    connect.connect(registry.byId(\"pageSelector_");

                                                            Write(lang);

WriteLiteral(@"""), ""onChange"", function (link) {
                        if (!link) {
                            return;
                        }
                        when(PermanentLinkHelper.getContent(link), function (content) {
                            query(""#contentId_");

                                          Write(lang);

WriteLiteral(@""")
                                .attr(""value"", content.contentLink);
                        });
                    });

                    new Grid({
                        ""class"": ""epi-grid-height--300 epi-grid--with-border"",
                        columns: {
                            phrase: """);

                                Write(Html.TranslateWithPath("phrase", localizationPath));

WriteLiteral("\",\r\n                            contentName: {\r\n                                l" +
"abel: \"");

                                   Write(Html.TranslateWithPath("contentId", localizationPath));

WriteLiteral("\",\r\n                                formatter: function (url, object) {\r\n        " +
"                            return \'<a");

WriteLiteral(" class=\"epi-visibleLink\"");

WriteLiteral(" target=\"_blank\"");

WriteAttribute("href", Tuple.Create(" href=\"", 7543), Tuple.Create("\"", 7630)
              , Tuple.Create(Tuple.Create("", 7550), Tuple.Create<System.Object, System.Int32>(Model.GetEditUrlPrefix(lang)
, 7550), false)
, Tuple.Create(Tuple.Create("", 7581), Tuple.Create("\'", 7581), true)
, Tuple.Create(Tuple.Create(" ", 7582), Tuple.Create("+", 7583), true)
, Tuple.Create(Tuple.Create(" ", 7584), Tuple.Create("object.contentId", 7585), true)
, Tuple.Create(Tuple.Create(" ", 7601), Tuple.Create("+", 7602), true)
, Tuple.Create(Tuple.Create(" ", 7603), Tuple.Create("object.contentProvider", 7604), true)
, Tuple.Create(Tuple.Create(" ", 7626), Tuple.Create("+", 7627), true)
, Tuple.Create(Tuple.Create(" ", 7628), Tuple.Create("\'", 7629), true)
);

WriteLiteral(">\' + object.contentName + \'</a> (\' + object.contentId + \')\';\r\n                   " +
"             }\r\n                            },\r\n                            url:" +
" {\r\n                                label: \"");

                                   Write(Html.TranslateWithPath("url", localizationPath));

WriteLiteral("\",\r\n                                formatter: function (url) {\r\n                " +
"                    return (\'<a");

WriteLiteral(" class=\"epi-visibleLink\"");

WriteLiteral(" target=\"_blank\"");

WriteLiteral(" href=\' + url + \'");

WriteLiteral(@">' + url + '</a>');
                                }
                            },
                            actions: {
                                label: """",
                                renderCell: function (object, value, node) {
                                    new Button({
                                            label: """);

                                               Write(Html.TranslateWithPath("delete", localizationPath));

WriteLiteral(@""",
                                            iconClass: ""dijitIcon epi-iconTrash"",
                                            onClick: function () {
                                                new Confirmation({
                                                        description: """);

                                                                 Write(Html.TranslateWithPath("confirmDelete", localizationPath));

WriteLiteral("\",\r\n                                                        title: \"");

                                                           Write(Html.TranslateWithPath("confirmDelete", localizationPath));

WriteLiteral("\",\r\n                                                        onAction: function (c" +
"onfirmed) {\r\n                                                            if (con" +
"firmed) {\r\n                                                                windo" +
"w.location = \"");

                                                                              Write(Url.Action("Delete", "ElasticBestBets"));

WriteLiteral("?index=");

                                                                                                                              Write(ViewBag.SelectedIndex);

WriteLiteral("&typeName=");

                                                                                                                                                                Write(ViewBag.TypeName);

WriteLiteral(@"&languageId="" + object.lang + ""&phrase="" + object.phrase + ""&contentId="" + object.contentId;
                                                            }
                                                        }
                                                    })
                                                    .show();
                                            }
                                        })
                                        .placeAt(node)
                                        .startup();
                                }
                            }
                        }
                    }, """);

                   Write(lang);

WriteLiteral("-bestbetsGrid\")\r\n                    .renderArray([\r\n");

                        
                         foreach (var bb in bestBetByLanguage.BestBets)
                        {

WriteLiteral("                            ");

WriteLiteral("\r\n                            {\r\n                                phrase: \"");

                                    Write(Html.Raw(bb.Phrase));

WriteLiteral("\",\r\n                                contentId: \"");

                                       Write(Html.Raw(bb.Id));

WriteLiteral("\",\r\n                                contentProvider: \"");

                                              Write(String.IsNullOrWhiteSpace(bb.Provider) ? null : "__" + Html.Raw(bb.Provider));

WriteLiteral("\",\r\n                                contentName: \"");

                                         Write(Html.Raw(bb.Name));

WriteLiteral("\",\r\n                                url: \"");

                                 Write(Html.Raw(bb.Url));

WriteLiteral("\",\r\n                                lang: \"");

                                  Write(lang);

WriteLiteral("\",\r\n                                actions: \"\"\r\n                            }\r\n " +
"                           ");

WriteLiteral("\r\n");

                            
                        Write(bb != bestBetByLanguage.BestBets.Last() ? "," : null);

                                                                                   
                        }

WriteLiteral("                    ]);\r\n                ");

WriteLiteral("\r\n");


            }

WriteLiteral(@"        }
        );
    }
</script>

<script>
    // At this point it's not safe to require() arbitrary things yet or everything will blow up spectacularly. The
    // ""Bootstrapper"" has to be run first, so only require that.
    require([""epi/shell/Bootstrapper""], function (Bootstrapper) {
        var bs = new Bootstrapper(");

                             Write(Html.Raw(Html.SerializeObject(Model.GetModuleSettings(), "application/json")));

WriteLiteral(");\r\n\r\n        // Loads the specified module (\"CMS\") and all the script bundles Cl" +
"ientResources that come with it. If this isn\'t done\r\n        // correctly all re" +
"quire() calls will load modules with separate requests which can reduce the amou" +
"nt of total code\r\n        // loaded but generates a *lot* of requests in the pro" +
"cess\r\n        bs.initializeApplication(null, \"CMS\").then(function () {\r\n        " +
"    // It\'s now safe to require() anything including your own modules.\r\n        " +
"    require([\r\n                \"dojo/_base/connect\",\r\n                \"dojo/pars" +
"er\",\r\n                \"epi-cms/ApplicationSettings\"\r\n            ], function (\r\n" +
"                connect,\r\n                parser,\r\n                ApplicationSe" +
"ttings\r\n            ) {\r\n                    // This sets the \"current context\" " +
"which is required by some controls such as the WYSIWYG.\r\n                    // " +
"It\'s used to show the current page media list as well as the \"Current page\" butt" +
"on in page selectors. This\r\n                    // just sets it to the root page" +
" so everything doesn\'t break.\r\n                    connect.publish(\"/epi/shell/c" +
"ontext/updateRequest\", [{ uri: \"epi.cms.contentdata:///\" + ApplicationSettings.r" +
"ootPage }]);\r\n                    // All done! Everything should be set up now. " +
"Run your own code here.\r\n                    // Should probably run this at some" +
" point as it\'s not done automatically - this initializes all the declarative\r\n  " +
"                  // widgets (elements with data-dojo-type). Use .then() if you " +
"want to run code after this to ensure everything has\r\n                    // fin" +
"ished executing.\r\n                    parser.parse()\r\n                        .t" +
"hen(loadScript)\r\n                        .then(function () {\r\n                  " +
"          dojo.attr(dojo.byId(\"loader\"), \"style\", \"display:none;\");\r\n           " +
"                 dojo.attr(dojo.byId(\"tabContainer\"), \"style\", \"\");\r\n           " +
"             });\r\n                });\r\n        });\r\n    });\r\n</script>");

        }
    }
}
#pragma warning restore 1591
