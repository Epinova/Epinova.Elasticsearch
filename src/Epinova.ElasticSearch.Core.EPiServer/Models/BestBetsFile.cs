using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "CB1F03BD-8D75-4E14-8CB9-33274627F429")]
    [AdministrationSettings(Visible = false, CodeOnly = true, GroupName = "mediatypes",
    PropertyDefinitionFields = PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^ PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [MediaDescriptor(ExtensionString = Extension)]
    [AvailableContentTypes(Availability = Availability.None)]
    internal class BestBetsFile : MediaData
    {
        internal const string Extension = "bestbets";
        public virtual string LanguageId { get; set; }
    }
}