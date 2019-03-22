using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;
using Xunit;

namespace Core.Episerver.Tests.Models.ViewModels.Abstractions
{
    public class LanguageAwareViewModelBaseTests
    {
        private class TestableLanguageAwareViewModelBase : LanguageAwareViewModelBase
        {
            public TestableLanguageAwareViewModelBase(string currentLanguage) : base(currentLanguage)
            {
            }
        }

        [Fact]
        public void Ctor_CurrentLanguageIsNull_ReturnsEmptyString()
        {
            var model = new TestableLanguageAwareViewModelBase(null);
            Assert.Empty(model.CurrentLanguage);
        }
    }
}