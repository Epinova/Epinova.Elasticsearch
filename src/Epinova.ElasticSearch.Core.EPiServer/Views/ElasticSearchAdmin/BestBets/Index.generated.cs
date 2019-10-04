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
    using Epinova.ElasticSearch.Core.Models.Admin;
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

WriteAttribute("src", Tuple.Create(" src=\"", 1463), Tuple.Create("\"", 1588)
, Tuple.Create(Tuple.Create("", 1469), Tuple.Create<System.Object, System.Int32>(EPiServer.Shell.Paths.ToShellClientResource("ClientResources/epi/themes/sleek/epi/images/icons/ajaxProgress-salt.gif")
, 1469), false)
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

WriteAttribute("title", Tuple.Create(" title=\"", 1859), Tuple.Create("\"", 1883)
, Tuple.Create(Tuple.Create("", 1867), Tuple.Create<System.Object, System.Int32>(bb.LanguageName
, 1867), false)
);

WriteLiteral(" data-dojo-props=\"");

                                                                                                 Write(bb.LanguageId == Model.CurrentLanguage ? "selected:true" : null);

WriteLiteral("\"");

WriteLiteral(">\r\n                <div");

WriteLiteral(" class=\"epi-padding-small\"");

WriteLiteral(">\r\n");

                    
                     if (bb.Indices.Count > 1)
                    {

WriteLiteral("                        <h2>");

                       Write(Html.TranslateWithPathRaw("index", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("                        <div");

WriteLiteral(" data-dojo-type=\"dijit/form/DropDownButton\"");

WriteLiteral(" class=\"epi-primary\"");

WriteLiteral(">\r\n                            <span>");

                             Write(ViewBag.SelectedIndexName);

WriteLiteral("</span>\r\n                            <div");

WriteLiteral(" data-dojo-type=\"dijit/DropDownMenu\"");

WriteLiteral(">\r\n");

                                
                                 foreach (var index in bb.Indices)
                                {
                                    var indexName = $"{index.Key}-{bb.LanguageId}";
                                    if (indexName != ViewBag.SelectedIndex)
                                    {

WriteLiteral("                                        <div");

WriteLiteral(" data-dojo-type=\"dijit/MenuItem\"");

WriteLiteral("\r\n                                             data-dojo-props=\"onClick:function(" +
"){document.location=\'?index=");

                                                                                                      Write(indexName);

WriteLiteral("&languageId=");

                                                                                                                            Write(bb.LanguageId);

WriteLiteral("\';}\"");

WriteLiteral(">\r\n");

WriteLiteral("                                            ");

                                       Write(index.Value);

WriteLiteral("\r\n                                        </div>\r\n");

                                    }
                                }

WriteLiteral("                            </div>\r\n                        </div>\r\n");

                    }

WriteLiteral("\r\n                    <h2>");

                   Write(Html.TranslateWithPath("newbestbet", localizationPath));

WriteLiteral("</h2>\r\n\r\n");

                    
                     using (Html.BeginForm("Add", "ElasticBestBets"))
                    {

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"LanguageId\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3463), Tuple.Create("\"", 3485)
, Tuple.Create(Tuple.Create("", 3471), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3471), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"Index\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3548), Tuple.Create("\"", 3578)
, Tuple.Create(Tuple.Create("", 3556), Tuple.Create<System.Object, System.Int32>(ViewBag.SelectedIndex
, 3556), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"TypeName\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3644), Tuple.Create("\"", 3669)
, Tuple.Create(Tuple.Create("", 3652), Tuple.Create<System.Object, System.Int32>(ViewBag.TypeName
, 3652), false)
);

WriteLiteral(" />\r\n");



WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-form-container__section__row epi-form-container__section__row--field\"" +
"");

WriteLiteral(">\r\n                            <label");

WriteAttribute("for", Tuple.Create(" for=\"", 3823), Tuple.Create("\"", 3850)
, Tuple.Create(Tuple.Create("", 3829), Tuple.Create("phrase_", 3829), true)
, Tuple.Create(Tuple.Create("", 3836), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3836), false)
);

WriteLiteral(">");

                                                          Write(Html.TranslateWithPathRaw("phrase", localizationPath));

WriteLiteral("</label>\r\n                            <input");

WriteAttribute("id", Tuple.Create(" id=\"", 3950), Tuple.Create("\"", 3976)
, Tuple.Create(Tuple.Create("", 3955), Tuple.Create("phrase_", 3955), true)
, Tuple.Create(Tuple.Create("", 3962), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 3962), false)
);

WriteLiteral(" data-dojo-type=\"dijit/form/ValidationTextBox\"");

WriteLiteral(" name=\"phrase\"");

WriteLiteral(" data-dojo-props=\"required:true\"");

WriteLiteral(" />\r\n                        </div>\r\n");

WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-form-container__section__row epi-form-container__section__row--field\"" +
"");

WriteLiteral(">\r\n                            <label");

WriteAttribute("for", Tuple.Create(" for=\"", 4252), Tuple.Create("\"", 4285)
, Tuple.Create(Tuple.Create("", 4258), Tuple.Create("pageSelector_", 4258), true)
, Tuple.Create(Tuple.Create("", 4271), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 4271), false)
);

WriteLiteral(">");

                                                                Write(Html.TranslateWithPathRaw("contentid", localizationPath));

WriteLiteral("</label>\r\n                            <div");

WriteAttribute("id", Tuple.Create(" id=\"", 4386), Tuple.Create("\"", 4418)
, Tuple.Create(Tuple.Create("", 4391), Tuple.Create("pageSelector_", 4391), true)
, Tuple.Create(Tuple.Create("", 4404), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 4404), false)
);

WriteLiteral("\r\n                                 class=\"pageSelector\"");

WriteLiteral("\r\n                                 data-dojo-type=\"epi-cms/widget/ContentSelector" +
"\"");

WriteLiteral("\r\n                                 data-dojo-props=\"showAllLanguages: false, root" +
"s: [\'");

                                                                               Write(String.Join("','", Model.SelectorRoots));

WriteLiteral("\'], repositoryKey: \'pages\', allowedTypes: [\'");

                                                                                                                                                                   Write(String.Join("','", Model.SelectorTypes));

WriteLiteral("\'], allowedDndTypes: [], value: null, required:true\"");

WriteLiteral(">\r\n                                <script");

WriteLiteral(" type=\"dojo/method\"");

WriteLiteral(" data-dojo-event=\"onChange\"");

WriteLiteral(">\r\n                                    dojo.byId(\'contentId_");

                                                    Write(bb.LanguageId);

WriteLiteral("\').value = this.value;\r\n                                </script>\r\n              " +
"              </div>\r\n                            <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"contentId\"");

WriteAttribute("id", Tuple.Create(" id=\"", 5148), Tuple.Create("\"", 5177)
, Tuple.Create(Tuple.Create("", 5153), Tuple.Create("contentId_", 5153), true)
, Tuple.Create(Tuple.Create("", 5163), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 5163), false)
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

WriteAttribute("id", Tuple.Create(" id=\"", 5654), Tuple.Create("\"", 5688)
, Tuple.Create(Tuple.Create("", 5659), Tuple.Create<System.Object, System.Int32>(bb.LanguageId
, 5659), false)
, Tuple.Create(Tuple.Create("", 5675), Tuple.Create("-bestbetsGrid", 5675), true)
);

WriteLiteral("></div>\r\n");

                    }

WriteLiteral("                </div>\r\n            </div>\r\n");

        }

WriteLiteral(@"    </div>
</div>


<script>
    // At this point it's not safe to require() arbitrary things yet or everything will blow up spectacularly. The
    // ""Bootstrapper"" has to be run first, so only require that.
    require([""epi/shell/Bootstrapper""], function (Bootstrapper) {
        var settings = ");

                  Write(Html.Raw(Html.SerializeObject(Model.GetModuleSettings(), "application/json")));

WriteLiteral(";\r\n        var bs = new Bootstrapper(settings);\r\n\r\n        // Loads the specified" +
" module (\"CMS\") and all the script bundles ClientResources that come with it. If" +
" this isn\'t done\r\n        // correctly all require() calls will load modules wit" +
"h separate requests which can reduce the amount of total code\r\n        // loaded" +
" but generates a *lot* of requests in the process\r\n        bs.initializeApplicat" +
"ion(null, \"CMS\").then(function () {\r\n\r\n            // It\'s now safe to require()" +
" anything including your own modules.\r\n            require([\r\n                \"d" +
"ojo/_base/connect\",\r\n                \"dgrid/Grid\",\r\n                \"dijit/form/" +
"Button\",\r\n                \"dijit/_Widget\",\r\n                \"dijit/_TemplatedMix" +
"in\",\r\n                \"epi/shell/widget/dialog/Confirmation\",\r\n                \"" +
"dojo/parser\",\r\n                \"dojo/topic\",\r\n                \"epi-cms/widget/Co" +
"ntentSelector\",\r\n                \"epi-cms/ApplicationSettings\"\r\n            ], f" +
"unction (\r\n                connect,\r\n                Grid,\r\n                Butt" +
"on,\r\n                _Widget,\r\n                _TemplatedMixin,\r\n               " +
" Confirmation,\r\n                parser,\r\n                topic,\r\n               " +
" ContentSelector,\r\n                ApplicationSettings\r\n            ) {\r\n       " +
"             // This sets the \"current context\" which is required by some contro" +
"ls such as the WYSIWYG.\r\n                    // It\'s used to show the current pa" +
"ge media list as well as the \"Current page\" button in page selectors. This\r\n    " +
"                // just sets it to the root page so everything doesn\'t break.\r\n " +
"                   topic.publish(\"/epi/cms/action/viewsettingvaluechanged\", \"vie" +
"wlanguage\", \"no\");\r\n                    connect.publish(\"/epi/shell/context/requ" +
"est\", [{ uri: \"epi.cms.contentdata:///\" + ApplicationSettings.startPage }]);\r\n  " +
"                  // All done! Everything should be set up now. Run your own cod" +
"e here.\r\n                    \r\n                    // Should probably run this a" +
"t some point as it\'s not done automatically - this initializes all the declarati" +
"ve\r\n                    // widgets (elements with data-dojo-type). Use .then() i" +
"f you want to run code after this to ensure everything has\r\n                    " +
"// finished executing.\r\n                    parser.parse()\r\n                    " +
"    .then(function () {\r\n");

                            
                             foreach (BestBetsByLanguage bestBetByLanguage in Model.BestBetsByLanguage)
                            {
                                string lang = bestBetByLanguage.LanguageId;

WriteLiteral("                                ");

WriteLiteral("\r\n                                    new Grid({\r\n                               " +
"         \"class\": \"epi-grid-height--300 epi-grid--with-border\",\r\n               " +
"                         columns: {\r\n                                           " +
" phrase: \"");

                                                Write(Html.TranslateWithPath("phrase", localizationPath));

WriteLiteral("\",\r\n                                            contentName: {\r\n                 " +
"                               label: \"");

                                                   Write(Html.TranslateWithPath("contentId", localizationPath));

WriteLiteral("\",\r\n                                                formatter: function (url, obj" +
"ect) {\r\n                                                    return \'<a");

WriteLiteral(" class=\"epi-visibleLink\"");

WriteLiteral(" target=\"_blank\"");

WriteAttribute("href", Tuple.Create(" href=\"", 9424), Tuple.Create("\"", 9511)
                              , Tuple.Create(Tuple.Create("", 9431), Tuple.Create<System.Object, System.Int32>(Model.GetEditUrlPrefix(lang)
, 9431), false)
, Tuple.Create(Tuple.Create("", 9462), Tuple.Create("\'", 9462), true)
, Tuple.Create(Tuple.Create(" ", 9463), Tuple.Create("+", 9464), true)
, Tuple.Create(Tuple.Create(" ", 9465), Tuple.Create("object.contentId", 9466), true)
, Tuple.Create(Tuple.Create(" ", 9482), Tuple.Create("+", 9483), true)
, Tuple.Create(Tuple.Create(" ", 9484), Tuple.Create("object.contentProvider", 9485), true)
, Tuple.Create(Tuple.Create(" ", 9507), Tuple.Create("+", 9508), true)
, Tuple.Create(Tuple.Create(" ", 9509), Tuple.Create("\'", 9510), true)
);

WriteLiteral(@">' + object.contentName + '</a> (' + object.contentId + ')';
                                                }
                                            },
                                            url: {
                                                label: """);

                                                   Write(Html.TranslateWithPath("url", localizationPath));

WriteLiteral("\",\r\n                                                formatter: function (url) {\r\n" +
"                                                    return (\'<a");

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

WriteLiteral("\",\r\n                                                                        title" +
": \"");

                                                                           Write(Html.TranslateWithPath("confirmDelete", localizationPath));

WriteLiteral(@""",
                                                                        onAction: function (confirmed) {
                                                                            if (confirmed) {
                                                                                window.location = """);

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

WriteLiteral("-bestbetsGrid\")\r\n                                    .renderArray([\r\n");

                                        
                                         foreach (var bb in bestBetByLanguage.BestBets)
                                        {

WriteLiteral("                                            ");

WriteLiteral("\r\n                                            {\r\n                                " +
"                phrase: \"");

                                                    Write(Html.Raw(bb.Phrase));

WriteLiteral("\",\r\n                                                contentId: \"");

                                                       Write(Html.Raw(bb.Id));

WriteLiteral("\",\r\n                                                contentProvider: \"");

                                                              Write(String.IsNullOrWhiteSpace(bb.Provider) ? null : "__" + Html.Raw(bb.Provider));

WriteLiteral("\",\r\n                                                contentName: \"");

                                                         Write(Html.Raw(bb.Name));

WriteLiteral("\",\r\n                                                url: \"");

                                                 Write(Html.Raw(bb.Url));

WriteLiteral("\",\r\n                                                lang: \"");

                                                  Write(lang);

WriteLiteral("\",\r\n                                                actions: \"\"\r\n                " +
"                            }\r\n                                            ");

WriteLiteral("\r\n");

                                            
                                        Write(bb != bestBetByLanguage.BestBets.Last() ? "," : null);

                                                                                                   
                                        }

WriteLiteral("                                    ]);\r\n                                ");

WriteLiteral("\r\n");

                            }

WriteLiteral(@"                        })
                        .then(function () {
                            dojo.attr(dojo.byId(""loader""), ""style"", ""display:none;"");
                            dojo.attr(dojo.byId(""tabContainer""), ""style"", """");
                        });
                });
        });
    });
</script>");

        }
    }
}
#pragma warning restore 1591
