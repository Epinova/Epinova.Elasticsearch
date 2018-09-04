using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "0000158F-E62F-4F4A-875B-A59DF096E956")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
        PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [AvailableContentTypes(Include = new[] { typeof(SynonymBackupFile) })]
    internal class SynonymBackupFileFolder : ContentFolder
    {
    }
}