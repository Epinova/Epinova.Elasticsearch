define([
    'dojo/topic',
    'dojo/_base/declare',
    'epi/dependency',
    'epi/shell/command/_Command',
    'epi/i18n!epi/nls/epinovaelasticsearch.widget'
], function (
    topic,
    declare,
    dependency,
    _Command,
    translator
) {
    return declare([_Command], {
        name: 'updateStructure',
        label: translator.button.label,
        tooltip: translator.button.tooltip,
        iconClass: 'epi-iconSortAscending',
        canExecute: true,

        _execute: function () {
            dojo.rawXhrPost({
                url: '/ElasticSearchAdmin/ElasticIndexer/UpdateItem',
                handleAs: 'json',
                headers: { "Content-Type": 'application/json' },
                timeout: 10000,
                postData: dojo.toJson({ "id": this.model.contentLink, "recursive": true }),
                load: function (data) {
                    if (!!console && !!console.info) {
                        console.info(data.status);
                    }
                },
                error: function (error) {
                    alert('An error occured, unable to update index. Status: ' + error);
                }
            });
        }
    });
});