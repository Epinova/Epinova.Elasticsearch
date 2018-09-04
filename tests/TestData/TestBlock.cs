using EPiServer.Core;

namespace TestData
{
    public class TestBlock : BlockData
    {
        public virtual string TestProp { get; set; }

        public virtual TestBlockInherited SubBlock { get; set; }
    }

    public class TestBlockInherited : TestBlock
    {
        public virtual string TestProp2 { get; set; }
    }
}