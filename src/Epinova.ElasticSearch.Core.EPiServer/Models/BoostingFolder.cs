using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "A93DA279-08AD-4D47-952D-70FD566B66C5")]
    [AdministrationSettings(Visible = false, CodeOnly = true,
        PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [AvailableContentTypes(Include = new[] { typeof(BoostingData) })]
    public class BoostingFolder : ContentFolder
    {
    }
}