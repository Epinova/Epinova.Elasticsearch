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
    using EPiServer.SpecializedProperties;
    using EPiServer.Web;
    using EPiServer.Web.Mvc.Html;
    using EPiServer.Web.Routing;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/ElasticSearchAdmin/Synonyms/Index.cshtml")]
    public partial class _Views_ElasticSearchAdmin_Synonyms_Index_cshtml : System.Web.Mvc.WebViewPage<SynonymsViewModel>
    {
        public _Views_ElasticSearchAdmin_Synonyms_Index_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

WriteLiteral("\r\n");

  
    ViewBag.ContainerClass = String.Empty;
    Layout = "~/Views/ElasticSearchAdmin/_ElasticSearch.cshtml";

WriteLiteral("\r\n\r\n");

 if (Model == null)
{
    return;
}

WriteLiteral("\r\n");

  
    string localizationPath = "/epinovaelasticsearch/synonyms/";

WriteLiteral("\r\n\r\n");

DefineSection("Styles", () => {

WriteLiteral(@"
    <style>
        #tabContainer .field-actions,
        #tabContainer .field-twoway {
            width: 120px;
            text-align: center;
        }

            #tabContainer .field-twoway .dijitCheckBoxDisabled {
                background-position: -15px;
            }

            #tabContainer .field-twoway .dijitCheckBoxCheckedDisabled {
                background-position: 0;
            }

            #tabContainer .field-twoway .dijitDisabled input {
                color: inherit;
            }
    </style>
");

});

WriteLiteral("\r\n<div");

WriteLiteral(" id=\"tabContainer\"");

WriteLiteral(">\r\n    <div");

WriteLiteral(" data-dojo-type=\"dijit/layout/TabContainer\"");

WriteLiteral(" doLayout=\"false\"");

WriteLiteral(">\r\n");

        
         foreach (LanguageSynonyms lang in Model.SynonymsByLanguage)
        {

WriteLiteral("            <div");

WriteLiteral(" data-dojo-type=\"dijit/layout/ContentPane\"");

WriteAttribute("title", Tuple.Create(" title=\"", 1302), Tuple.Create("\"", 1328)
, Tuple.Create(Tuple.Create("", 1310), Tuple.Create<System.Object, System.Int32>(lang.LanguageName
, 1310), false)
);

WriteLiteral(" data-dojo-props=\"");

                                                                                                   Write(lang.LanguageId == Model.CurrentLanguage ? "selected:true" : null);

WriteLiteral("\"");

WriteLiteral(">\r\n");

                
                 if (lang.HasSynonymsFile)
                {

WriteLiteral("                    <div");

WriteLiteral(" data-dojo-attach-point=\"notificationBarNode\"");

WriteLiteral(" class=\"epi-notificationBar dijitVisible\"");

WriteLiteral(">\r\n                        <div");

WriteLiteral(" class=\"epi-notificationBarItem\"");

WriteLiteral(">\r\n                            <div");

WriteLiteral(" class=\"epi-notificationBarText\"");

WriteLiteral(">\r\n                                <p>");

                              Write(Html.TranslateWithPath("synonymfilenotice", localizationPath));

WriteLiteral("</p>\r\n                            </div>\r\n                        </div>\r\n       " +
"             </div>\r\n");

                }

WriteLiteral("\r\n                <div");

WriteLiteral(" class=\"epi-padding-small\"");

WriteLiteral(">\r\n");

                    
                     if (lang.Indices.Count > 1)
                    {

WriteLiteral("                        <div");

WriteLiteral(" class=\"epi-groupedButtonContainer\"");

WriteLiteral(">\r\n                            <h2>");

                           Write(Html.TranslateWithPathRaw("index", localizationPath));

WriteLiteral("</h2>\r\n\r\n");

                            
                             foreach (var index in lang.Indices)
                            {
                                var indexName = $"{index.Key}-{lang.LanguageId}";
                                if (indexName == ViewBag.SelectedIndex)
                                {

WriteLiteral("                                    <span>");

                                     Write(index.Value);

WriteLiteral("</span>\r\n");

                                }
                                else
                                {

WriteLiteral("                                    <a");

WriteLiteral(" class=\"epi-visibleLink\"");

WriteAttribute("href", Tuple.Create(" href=\"", 2748), Tuple.Create("\"", 2800)
, Tuple.Create(Tuple.Create("", 2755), Tuple.Create("?index=", 2755), true)
, Tuple.Create(Tuple.Create("", 2762), Tuple.Create<System.Object, System.Int32>(indexName
, 2762), false)
, Tuple.Create(Tuple.Create("", 2772), Tuple.Create("&languageId=", 2772), true)
                  , Tuple.Create(Tuple.Create("", 2784), Tuple.Create<System.Object, System.Int32>(lang.LanguageId
, 2784), false)
);

WriteLiteral(">");

                                                                                                               Write(index.Value);

WriteLiteral("</a>\r\n");

                                }
                            }

WriteLiteral("                        </div>\r\n");

                    }

WriteLiteral("\r\n");

                    
                     using (Html.BeginForm("Add", "ElasticSynonyms"))
                    {

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"Index\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3094), Tuple.Create("\"", 3117)
, Tuple.Create(Tuple.Create("", 3102), Tuple.Create<System.Object, System.Int32>(lang.IndexName
, 3102), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"Analyzer\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3183), Tuple.Create("\"", 3205)
, Tuple.Create(Tuple.Create("", 3191), Tuple.Create<System.Object, System.Int32>(lang.Analyzer
, 3191), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("                        <input");

WriteLiteral(" type=\"hidden\"");

WriteLiteral(" name=\"LanguageId\"");

WriteAttribute("value", Tuple.Create(" value=\"", 3273), Tuple.Create("\"", 3297)
, Tuple.Create(Tuple.Create("", 3281), Tuple.Create<System.Object, System.Int32>(lang.LanguageId
, 3281), false)
);

WriteLiteral(" />\r\n");



WriteLiteral("                        <h2>");

                       Write(Html.TranslateWithPath("newsynonym", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("                        <p>\r\n                            <input");

WriteLiteral(" data-dojo-type=\"dijit/form/ValidationTextBox\"");

WriteLiteral(" name=\"from\"");

WriteAttribute("id", Tuple.Create(" id=\"", 3516), Tuple.Create("\"", 3542)
, Tuple.Create(Tuple.Create("", 3521), Tuple.Create("from_", 3521), true)
                      , Tuple.Create(Tuple.Create("", 3526), Tuple.Create<System.Object, System.Int32>(lang.LanguageId
, 3526), false)
);

WriteLiteral(" data-dojo-props=\"placeholder:\'");

                                                                                                                                                 Write(Html.TranslateWithPathRaw("from", localizationPath));

WriteLiteral("\',required:true,intermediateChanges:true\"");

WriteLiteral(" />\r\n                            <input");

WriteLiteral(" data-dojo-type=\"dijit/form/ValidationTextBox\"");

WriteLiteral(" name=\"to\"");

WriteLiteral(" data-dojo-props=\"placeholder:\'");

                                                                                                                    Write(Html.TranslateWithPathRaw("to", localizationPath));

WriteLiteral("\',required:true\"");

WriteLiteral(" />\r\n                            <input");

WriteLiteral(" type=\"radio\"");

WriteLiteral(" name=\"twoway\"");

WriteLiteral(" value=\"true\"");

WriteLiteral(" data-dojo-type=\"dijit/form/CheckBox\"");

WriteLiteral(" /> ");

                                                                                                              Write(Html.TranslateWithPath("twoway", localizationPath));

WriteLiteral("\r\n                            <button");

WriteLiteral(" data-dojo-type=\"dijit/form/Button\"");

WriteLiteral(" type=\"submit\"");

WriteLiteral(" class=\"epi-primary\"");

WriteLiteral(">");

                                                                                                    Write(Html.TranslateWithPath("addnew", localizationPath));

WriteLiteral("</button>\r\n                        </p>\r\n");

WriteLiteral("                        <p><em><small>");

                                 Write(Html.TranslateWithPath("newsynonyminfo", localizationPath));

WriteLiteral("</small></em></p>\r\n");



WriteLiteral("                        <h2>");

                       Write(Html.TranslateWithPath("existingsynonyms", localizationPath));

WriteLiteral("</h2>\r\n");

WriteLiteral("                        <div");

WriteAttribute("id", Tuple.Create(" id=\"", 4471), Tuple.Create("\"", 4507)
, Tuple.Create(Tuple.Create("", 4476), Tuple.Create<System.Object, System.Int32>(lang.LanguageId
, 4476), false)
, Tuple.Create(Tuple.Create("", 4494), Tuple.Create("-synonymsGrid", 4494), true)
);

WriteLiteral("></div>\r\n");

                    }

WriteLiteral("                </div>\r\n            </div>\r\n");

        }

WriteLiteral("    </div>\r\n</div>\r\n\r\n<script>\r\n    require(\r\n        [\"dojo/_base/declare\", \"dgr" +
"id/Grid\", \"dijit/form/Button\", \"dijit/form/CheckBox\", \"dojo/domReady!\"],\r\n      " +
"  function (declare, Grid, Button, CheckBox) {\r\n");

            
             foreach (LanguageSynonyms lang in Model.SynonymsByLanguage)
            {

WriteLiteral("                ");

WriteLiteral("\r\n                new Grid({\r\n                        \"class\": \"epi-grid-height--" +
"300 epi-grid--with-border\",\r\n                        columns: {\r\n               " +
"             from: \"");

                              Write(Html.TranslateWithPath("from", localizationPath));

WriteLiteral("\",\r\n                            to: \"");

                            Write(Html.TranslateWithPath("to", localizationPath));

WriteLiteral("\",\r\n                            twoway: {\r\n                                label:" +
" \"");

                                   Write(Html.TranslateWithPath("twoway", localizationPath));

WriteLiteral(@""",
                                renderCell: function (object, value, node) {
                                    if (object.multi) {
                                        return null;
                                    }

                                    new CheckBox({
                                        name: ""twoway"",
                                        checked: object.twoway,
                                        disabled: true
                                    })
                                    .placeAt(node)
                                    .startup();
                                }
                            },
                            actions: {
                                label: """",
                                renderCell: function (object, value, node) {
                                    new Button({
                                            label: """);

                                               Write(Html.TranslateWithPath("delete", localizationPath));

WriteLiteral("\",\r\n                                            iconClass: \"dijitIcon epi-iconTra" +
"sh\",\r\n                                            onClick: function() {\r\n       " +
"                                         if (confirm(\"");

                                                        Write(Html.TranslateWithPath("confirmDelete", localizationPath));

WriteLiteral("\")) {\r\n                                                    window.location = \"");

                                                                  Write(Url.Action("Delete", "ElasticSynonyms"));

WriteLiteral("?index=");

                                                                                                                  Write(ViewBag.SelectedIndex);

WriteLiteral("&languageId=\" + object.lang + \"&analyzer=");

                                                                                                                                                                                   Write(lang.Analyzer);

WriteLiteral(@"&from="" + object.from + ""&to="" + object.to + ""&twoway="" + object.twoway + ""&multiword="" + object.multi;
                                                }
                                            }
                                        })
                                        .placeAt(node)
                                        .startup();
                                }
                            }
                        }
                    }, """);

                    Write(lang.LanguageId);

WriteLiteral("-synonymsGrid\")\r\n                    .renderArray([\r\n");

                        
                         foreach (var synonym in lang.Synonyms)
                        {

WriteLiteral("                            ");

WriteLiteral("\r\n                                {\r\n                                    from: \"");

                                      Write(Html.Raw(synonym.From));

WriteLiteral("\",\r\n                                    to: \"");

                                    Write(Html.Raw(synonym.To));

WriteLiteral("\",\r\n                                    twoway: ");

                                       Write(synonym.TwoWay.ToString().ToLower());

WriteLiteral(",\r\n                                    multi: ");

                                      Write(synonym.MultiWord.ToString().ToLower());

WriteLiteral(",\r\n                                    lang: \"");

                                      Write(lang.LanguageId);

WriteLiteral("\",\r\n                                    actions: \"\"\r\n                            " +
"    }\r\n                            ");

WriteLiteral("\r\n");

                            
                        Write(synonym != lang.Synonyms.Last() ? "," : null);

                                                                           
                        }

WriteLiteral("                    ]);\r\n                ");

WriteLiteral("\r\n");

            }

WriteLiteral("        }\r\n    );\r\n</script>");

        }
    }
}
#pragma warning restore 1591
