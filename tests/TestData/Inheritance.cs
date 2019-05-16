using EPiServer.Core;

namespace TestData
{
    public class TestClassA : TestClassB, ITestInterfaceA
    {
    }

    public class TestClassB : TestClassC
    {
    }

    public class TestClassC : TestClassD
    {
    }

    public class TestClassD : BasicContent
    {
        public TestClassD()
        {
            Property = new PropertyDataCollection();
        }
    }

    public interface ITestInterfaceA
    {
    }
}
