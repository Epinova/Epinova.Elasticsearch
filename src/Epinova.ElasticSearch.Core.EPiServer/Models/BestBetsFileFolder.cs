using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "BB777539-A342-4A4D-A190-18EA42636215")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
        PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [AvailableContentTypes(Include = new[] { typeof(BestBetsFile) })]
    internal class BestBetsFileFolder : ContentFolder
    {
        internal const string ContentName = "Elasticsearch Best Bets";
    }
}