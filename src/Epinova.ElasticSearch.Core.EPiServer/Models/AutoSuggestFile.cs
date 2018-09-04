using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "0859284D-8570-4142-A1AC-3777AEA7B79C")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
    PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [MediaDescriptor(ExtensionString = "autosuggest")]
    [AvailableContentTypes(Availability = Availability.None)]
    internal class AutoSuggestFile : MediaData
    {
        public virtual string LanguageId { get; set; }
    }
}