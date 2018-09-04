using System;
using EPiServer.Core;

namespace TestData
{
    public class TypeWithoutBoosting : ContentData, IContent
    {
        public string NormalProperty { get; set; }
        public string Name { get; set; }
        public ContentReference ContentLink { get; set; }
        public ContentReference ParentLink { get; set; }
        public Guid ContentGuid { get; set; }
        public int ContentTypeID { get; set; }
        public bool IsDeleted { get; set; }
    }
}
