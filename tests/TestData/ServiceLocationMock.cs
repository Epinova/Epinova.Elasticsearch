using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Moq;

namespace TestData
{
    public class ServiceLocationMock
    {
        public Mock<IServiceLocator> ServiceLocatorMock { get; set; }
        public Mock<IPublishedStateAssessor> StateAssesorMock { get; set; }
        public ITemplateResolver TemplateResolver { get; set; }
    }
}
