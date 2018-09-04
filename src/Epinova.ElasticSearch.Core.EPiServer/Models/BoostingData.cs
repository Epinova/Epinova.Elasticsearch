using System;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    [ContentType(GUID = "AF034397-F180-4CE0-B057-286152AA52B2")]
    [AdministrationSettings(Visible = false, CodeOnly = true,
        PropertyDefinitionFields =
            PropertyDefinitionFields.All ^ PropertyDefinitionFields.DisplayEditUI ^
            PropertyDefinitionFields.LanguageSpecific ^ PropertyDefinitionFields.Searchable)]
    [AvailableContentTypes(Availability = Availability.None)]
    internal class BoostingData : IContent
    {
        public BoostingData()
        {
            Property = new PropertyDataCollection();
        }

        public virtual string FieldName { get; set; }
        public virtual int Weight { get; set; }

        // IContent
        public string Name { get; set; }
        public ContentReference ContentLink { get; set; }
        public ContentReference ParentLink { get; set; }
        public Guid ContentGuid { get; set; }
        public int ContentTypeID { get; set; }
        public bool IsDeleted { get; set; }
        public PropertyDataCollection Property { get; }
    }
}