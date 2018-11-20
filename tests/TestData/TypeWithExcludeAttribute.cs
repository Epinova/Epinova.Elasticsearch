using System;
using Epinova.ElasticSearch.Core.Attributes;
using EPiServer.Core;

namespace TestData
{
    [ExcludeFromSearch]
    public class TypeWithExcludeAttribute : ContentData, IContent
    {
        public TypeWithExcludeAttribute()
        {
            ContentLink = Factory.GetPageReference();
        }

        #region IContent implementation

        public string Name
        {
            get { return null; }
            set { }
        }

        public ContentReference ContentLink { get; set; }

        public ContentReference ParentLink
        {
            get { return null; }
            set { }
        }

        public Guid ContentGuid
        {
            get { return new Guid(); }
            set { }
        }

        public int ContentTypeID
        {
            get { return 0; }
            set { }
        }

        public bool IsDeleted
        {
            get { return false; }
            set { }
        }

        #endregion

    }
}
