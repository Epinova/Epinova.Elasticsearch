using EPiServer.Core;
using EPiServer.Framework.DataAnnotations;

namespace TestData
{
    [MediaDescriptor(ExtensionString = "docx,pdf")]
    public class TestMedia : MediaData
    {
    }
}
