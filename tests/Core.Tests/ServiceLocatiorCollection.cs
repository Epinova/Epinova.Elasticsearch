using TestData;
using Xunit;

namespace Core.Tests
{
    [CollectionDefinition(nameof(ServiceLocatiorCollection))]
    public class ServiceLocatiorCollection : ICollectionFixture<ServiceLocatorFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
