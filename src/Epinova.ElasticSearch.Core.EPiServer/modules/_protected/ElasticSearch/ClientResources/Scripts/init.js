define([
    'dojo',
    'epi/dependency',
    'epi-cms/plugin-area/navigation-tree',
    'epinova-elasticsearch/ToolbarProvider',
    'epinova-elasticsearch/UpdateTreeStructureCommand'
],
function (
    dojo,
    dependency,
    navigationTreePluginArea,
    ToolbarProvider,
    UpdateTreeStructureCommand
) {
    return dojo.declare([], {
        initialize: function () {
            var commandregistry = dependency.resolve('epi.globalcommandregistry');
            commandregistry.registerProvider('epi.cms.contentdetailsmenu', new ToolbarProvider());
            navigationTreePluginArea.add(UpdateTreeStructureCommand);
        }
    });
});