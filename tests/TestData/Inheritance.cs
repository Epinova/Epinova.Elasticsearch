using System;
using EPiServer.Core;

namespace TestData
{
    public class TestClassA : TestClassB, IContent, ITestInterfaceA
    {
        public PropertyDataCollection Property => new PropertyDataCollection();

        #region IContent implementation

        public string Name
        {
            get { return null; }
            set { }
        }

        public ContentReference ContentLink
        {
            get { return null; }
            set { }
        }

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

    public class TestClassB : TestClassC
    {
    }

    public class TestClassC : TestClassD
    {
    }

    public class TestClassD
    {
    }

    public interface ITestInterfaceA
    {
    }
}
