define([
    'dojo/_base/declare',
    'dijit/form/Button',
    'epi-cms/component/command/_GlobalToolbarCommandProvider',
    'epi/i18n!epi/nls/epinovaelasticsearch.widget',
    'epinova-elasticsearch/UpdateIndexCommand'
],
function (
    declare,
    Button,
    _GlobalToolbarCommandProvider,
    translator,
    UpdateIndexCommand
) {
    return declare([_GlobalToolbarCommandProvider], {
        constructor: function () {
            this.inherited(arguments);

            this.addToCenter(new UpdateIndexCommand({ label: translator.button.label }), { showLabel: true, widget: Button });
        }
    });
});