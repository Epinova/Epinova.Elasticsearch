using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "151C521D-5A01-4CC0-8EEC-8808F92D8F85")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
    PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [MediaDescriptor(ExtensionString = "synonyms")]
    [AvailableContentTypes(Availability = Availability.None)]
    internal class SynonymBackupFile : MediaData
    {
    }
}