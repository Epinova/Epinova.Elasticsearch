define([
    "dojo/_base/declare",
    "dojo/dom-geometry",
    "dijit/_TemplatedMixin",
    "dijit/_Container",
    "dijit/layout/_LayoutWidget",
    "epi-cms/component/ContentQueryGrid",
    "epi-cms/_ContentContextMixin",
    "dijit/_WidgetsInTemplateMixin",
    "dojo/html",
    'epi/i18n!epi/nls/epinovaelasticsearch.components.mlt'
], function (
    declare,
    domGeometry,
    _TemplatedMixin,
    _Container,
    _LayoutWidget,
    ContentQueryGrid,
    _ContentContextMixin,
    _WidgetsInTemplateMixin,
    html,
    translator
) {
        return declare([
            _Container,
            _LayoutWidget,
            _TemplatedMixin,
            _WidgetsInTemplateMixin,
            _ContentContextMixin],
            {
                templateString: '<div>\
                                    <div class="epi-gadgetInnerToolbar" data-dojo-attach-point="toolbar">\
                                        <div>' + translator.prefix + ' «<span data-dojo-attach-point="contentName"></span>»</div>\
                                    </div>\
                                    <div data-dojo-type="epi-cms/component/ContentQueryGrid" data-dojo-attach-point="contentQuery"></div>\
                                </div>',

                resize: function (newSize) {
                    this.inherited(arguments);
                    var toolbarSize = domGeometry.getMarginBox(this.toolbar);
                    var gridSize = { w: newSize.w, h: newSize.h - toolbarSize.h }
                    this.contentQuery.resize(gridSize);
                },

                postMixInProperties: function () {
                    this.getCurrentContent().then(function (context) {
                        this._updateUI(context);
                    }.bind(this));
                },

                contextChanged: function (context, callerData) {
                    this.inherited(arguments);

                    // the context changed, probably because we navigated or published something
                    this._updateUI(context);
                },

                _updateUI: function (context) {
                    html.set(this.contentName, context.name);

                    this.contentQuery.fetchData = this.fetchData;
                    this.contentQuery.set("queryParameters", { queryText: context.contentLink || context.id });
                    this.contentQuery.set("queryName", "MoreLikeThisQuery");
                },

                fetchData: function () {
                    this.grid.set("queryOptions", {
                        ignore: ["query"]
                    });

                    var queryParameters = this.queryParameters || {};
                    queryParameters.query = this.queryName;

                    this.grid.set("query", queryParameters);
                }
            });
    });