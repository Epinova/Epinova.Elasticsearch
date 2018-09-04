using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "50A5C46D-51DA-4643-88F7-8959CD37C9AC")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
        PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [AvailableContentTypes(Include = new[] { typeof(AutoSuggestFile) })]
    internal class AutoSuggestFileFolder : ContentFolder
    {
    }
}