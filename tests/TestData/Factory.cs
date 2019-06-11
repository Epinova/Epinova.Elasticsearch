using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess.Internal;
using EPiServer.Framework.Blobs;
using EPiServer.Framework.Web;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Moq;

namespace TestData
{
    public static class Factory
    {
        private static readonly Random Random = new Random();


        public static ServiceLocationMock ConfigureStructureMap()
        {
            ServiceLocationMock result = new ServiceLocationMock
            {
                ServiceLocatorMock = new Mock<IServiceLocator>(),
                StateAssesorMock = new Mock<IPublishedStateAssessor>(),
                TemplateResolver = GetTemplateResolver()
            };

            result.ServiceLocatorMock.Setup(m => m.GetInstance<IPublishedStateAssessor>()).Returns(result.StateAssesorMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ITemplateResolver>()).Returns(result.TemplateResolver);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ContentAssetHelper>()).Returns(new Mock<ContentAssetHelper>().Object);

            ServiceLocator.SetLocator(result.ServiceLocatorMock.Object);
            return result;
        }

        public static ServiceLocationMock SetupServiceLocator(string testHost = null, string username = null, string password = null)
        {
            ServiceLocationMock result = new ServiceLocationMock
            {
                ServiceLocatorMock = new Mock<IServiceLocator>(),
                StateAssesorMock = new Mock<IPublishedStateAssessor>(),
                TemplateResolver = GetTemplateResolver()
            };

            result.StateAssesorMock = new Mock<IPublishedStateAssessor>();
            result.StateAssesorMock
                .Setup(m => m.IsPublished(It.IsAny<IContent>(), It.IsAny<PublishedStateCondition>()))
                .Returns(true);

            Mock<ContentPathDB> contentPathMock = new Mock<ContentPathDB>(new Mock<IDatabaseExecutor>().Object);
            Mock<IBestBetsRepository> bestbetMock = new Mock<IBestBetsRepository>();
            Mock<IBoostingRepository> boostMock = new Mock<IBoostingRepository>();
            boostMock
                .Setup(m => m.GetByType(It.IsAny<Type>()))
                .Returns(new Dictionary<string, int>());

            Mock<ILanguageBranchRepository> language = new Mock<ILanguageBranchRepository>();
            language.Setup(m => m.ListEnabled()).Returns(new List<LanguageBranch>
            {
                new LanguageBranch(new CultureInfo("no"))
            });


            Mock<IElasticSearchSettings> settings = new Mock<IElasticSearchSettings>();
            settings.Setup(m => m.BulkSize).Returns(1000);
            settings.Setup(m => m.CloseIndexDelay).Returns(2000);
            if(username != null)
                settings.Setup(m => m.Username).Returns(username);
            if(password != null)
                settings.Setup(m => m.Password).Returns(password);
            settings.Setup(m => m.EnableFileIndexing).Returns(true);
            settings.Setup(m => m.IgnoreXhtmlStringContentFragments).Returns(false);
            settings.Setup(m => m.Index).Returns(ElasticFixtureSettings.IndexName);
            settings.Setup(m => m.GetLanguage(It.IsAny<string>())).Returns("no");
            settings.Setup(m => m.GetDefaultIndexName(It.IsAny<string>()))
                .Returns(ElasticFixtureSettings.IndexName);
            if (testHost != null)
                settings.Setup(m => m.Host).Returns(testHost.TrimEnd('/'));

            Mock<IIndexer> indexer = new Mock<IIndexer>();

            result.ServiceLocatorMock.Setup(m => m.GetInstance<IIndexer>()).Returns(indexer.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IBoostingRepository>()).Returns(boostMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IBestBetsRepository>()).Returns(bestbetMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IElasticSearchSettings>()).Returns(settings.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ILanguageBranchRepository>()).Returns(language.Object);
            //result.ServiceLocatorMock.Setup(m => m.GetInstance<IContentAccessEvaluator>()).Returns(new Mock<IContentAccessEvaluator>().Object);
            //result.ServiceLocatorMock.Setup(m => m.GetInstance<IPrincipalAccessor>()).Returns(new Mock<IPrincipalAccessor>().Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IElasticSearchService>()).Returns(new ElasticSearchService(settings.Object));

            result.ServiceLocatorMock.Setup(m => m.GetInstance<IPublishedStateAssessor>()).Returns(result.StateAssesorMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ITemplateResolver>()).Returns(result.TemplateResolver);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ContentPathDB>()).Returns(contentPathMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ContentAssetHelper>()).Returns(new Mock<ContentAssetHelper>().Object);

            ServiceLocator.SetLocator(result.ServiceLocatorMock.Object);
            return result;
        }

        public static string GetString(int size = 20)
        {
            string instance = Guid.NewGuid().ToString("N");

            while (instance.Length < size)
                instance += Guid.NewGuid().ToString("N");

            return instance.Substring(0, size);
        }

        public static string GetSentence(int words = 5)
        {
            string instance = String.Empty;

            for (int i = 0; i < words; i++)
                instance += GetString(Random.Next(3, 8)) + " ";

            return instance.Trim();
        }

        public static PageData GetPageData(
            bool visibleInMenu = true,
            bool isPublished = true,
            bool userHasAccess = true,
            bool hasTemplate = true,
            bool isNotInWaste = true,
            PageShortcutType shortcutType = PageShortcutType.Normal,
            int id = 0,
            int parentId = 0)
        {
            return GetPageData<PageData>(visibleInMenu, isPublished, userHasAccess, hasTemplate, isNotInWaste, shortcutType, id, parentId);
        }

        public static PageReference GetPageReference()
        {
            return new PageReference(GetInteger(1000));
        }

        public static int GetInteger(int start = 1, int count = 1337)
        {
            return Enumerable.Range(start, count).OrderBy(n => Guid.NewGuid()).First();
        }

        public static TemplateResolver GetTemplateResolver(bool hasTemplate = true)
        {
            Mock<TemplateResolver> templateResolver = new Mock<TemplateResolver>();

            templateResolver
                .Setup(
                    m =>
                        m.Resolve(
                            It.IsAny<HttpContextBase>(),
                            It.IsAny<Type>(),
                            It.IsAny<object>(),
                            It.IsAny<TemplateTypeCategories>(),
                            It.IsAny<string>()))
                .Returns(hasTemplate ? new TemplateModel() : null);
            templateResolver
                .Setup(m => m.HasTemplate(It.IsAny<IContentData>(), It.IsAny<TemplateTypeCategories>()))
                .Returns(hasTemplate);
            templateResolver
                .Setup(m => m.HasTemplate(It.IsAny<IContentData>(), It.IsAny<TemplateTypeCategories>(), It.IsAny<string>()))
                .Returns(hasTemplate);
            templateResolver
                .Setup(m => m.HasTemplate(It.IsAny<IContentData>(), It.IsAny<TemplateTypeCategories>(), It.IsAny<ContextMode>()))
                .Returns(hasTemplate);
            return templateResolver.Object;
        }

        public static T GetPageData<T>(
            bool visibleInMenu = true,
            bool isPublished = true,
            bool userHasAccess = true,
            bool hasTemplate = true,
            bool isNotInWaste = true,
            PageShortcutType shortcutType = PageShortcutType.Normal,
            int id = 0,
            int parentId = 0,
            CultureInfo language = null) where T : PageData
        {
            Mock<ISecurityDescriptor> securityDescriptor = new Mock<ISecurityDescriptor>();
            securityDescriptor.Setup(m => m.HasAccess(It.IsAny<IPrincipal>(), It.IsAny<AccessLevel>())).Returns(userHasAccess);

            if (language == null)
                language = CultureInfo.CurrentCulture;

            PageReference pageLink = id > 0 ? new PageReference(id) : GetPageReference();
            PageReference parentLink = parentId > 0 ? new PageReference(parentId) : GetPageReference();

            Guid pageGuid = Guid.NewGuid();
            Mock<T> instance = new Mock<T>();
            instance.SetupAllProperties();
            instance.Setup(m => m.VisibleInMenu).Returns(visibleInMenu);
            instance.Setup(m => m.Status).Returns(isPublished ? VersionStatus.Published : VersionStatus.NotCreated);
            instance.Setup(m => m.GetSecurityDescriptor()).Returns(securityDescriptor.Object);
            instance.Setup(m => m.ContentGuid).Returns(pageGuid);
            instance.Setup(m => m.ParentLink).Returns(parentLink);
            instance.Setup(m => m.ContentLink).Returns(pageLink);
            instance.Setup(m => m.Language).Returns(language);
            instance.Setup(m => m.Property).Returns(new PropertyDataCollection());
            instance.Setup(m => m.StaticLinkURL).Returns($"/link/{pageGuid:N}.aspx?id={pageLink.ID}");

            ContentReference.WasteBasket = new PageReference(1);
            if (!isNotInWaste)
                instance.Setup(m => m.ContentLink).Returns(ContentReference.WasteBasket);

            instance.Object.LinkType = shortcutType;
            return instance.Object;
        }

        public static TestPage GetTestPage(int id = 0, int parentId = 0, PageShortcutType shortcutType = PageShortcutType.Normal)
        {
            id = id > 0 ? id : GetInteger();
            parentId = parentId > 0 ? parentId : GetInteger();

            return new TestPage
            {
                Property =
                {
                    ["PageName"] = new PropertyString(),
                    ["PageStartPublish"] = new PropertyDate(new DateTime(3000, 1, 1)),
                    ["PageStopPublish"] = new PropertyDate(),
                    ["PageLink"] = new PropertyPageReference(id),
                    ["PageParentLink"] = new PropertyPageReference(parentId),
                    ["PageShortcutType"] = new PropertyNumber((int) shortcutType)
                }
            };
        }

        public static string GetJsonTestData(string filename)
        {
            string path = GetFilePath("Json", filename);

            if (!File.Exists(path))
                return String.Empty;

            return File.ReadAllText(path);
        }

        public static TestMedia GetMediaData(string name, string ext)
        {
            PageReference pageLink = GetPageReference();
            PageReference parentLink = GetPageReference();
            Guid pageGuid = Guid.NewGuid();
            FileBlob blob = new FileBlob(new Uri("foo://bar.com/"), GetFilePath("Media", name + "." + ext));

            Mock<TestMedia> instance = new Mock<TestMedia>();
            instance.SetupAllProperties();
            instance.Setup(m => m.BinaryData).Returns(blob);
            instance.Setup(m => m.Name).Returns(GetString(5) + "." + ext);
            instance.Setup(m => m.ContentGuid).Returns(pageGuid);
            instance.Setup(m => m.ParentLink).Returns(parentLink);
            instance.Setup(m => m.ContentLink).Returns(pageLink);
            instance.Setup(m => m.Property).Returns(new PropertyDataCollection());
            return instance.Object;
        }

        public static XhtmlString GetXhtmlString(string s, params IStringFragment[] additionalFragments)
        {
            if (s == null)
                return null;

            Mock<IStringFragment> fragmentMock = new Mock<IStringFragment>();
            fragmentMock.SetupAllProperties();
            fragmentMock.Setup(m => m.ToString()).Returns(s);
            fragmentMock.Setup(m => m.GetViewFormat()).Returns(s);

            Mock<XhtmlString> xhtmlStringMock = new Mock<XhtmlString>();
            StringFragmentCollection fragments = new StringFragmentCollection();
            if (!String.IsNullOrWhiteSpace(s))
                fragments.Add(fragmentMock.Object);

            if(additionalFragments != null && additionalFragments.Length > 0)
                foreach (IStringFragment fragment in additionalFragments)
                    fragments.Add(fragment);

            //xhtmlStringMock.Setup(m => m.FragmentParser).Returns(new Mock<Injected<IFragmentParser>>().Object);
            xhtmlStringMock.Setup(m => m.Fragments).Returns(fragments);
            xhtmlStringMock.Setup(m => m.ToString()).Returns(s);
            xhtmlStringMock.Setup(m => m.ToHtmlString()).Returns(s);
            xhtmlStringMock.Setup(m => m.ToInternalString()).Returns(s);
            xhtmlStringMock.Setup(m => m.ToEditString()).Returns(s);
            return xhtmlStringMock.Object;
        }

        private static string GetFilePath(string folder, string filename)
        {
            var appDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var jsonDir = Path.Combine(appDir.Parent?.Parent?.Parent?.FullName ?? String.Empty, "Tests", "TestData", folder);

            return Path.Combine(jsonDir, filename);
        }

        public static bool ArrayEquals<T1, T2>(IEnumerable<T1> arr1, IEnumerable<T2> arr2) //where T : struct
        {
            var obj1 = arr1.Select(x => x as object);
            var obj2 = arr2.Select(x => x as object);
            return obj1.SequenceEqual(obj2);
        }

        public static string RemoveWhitespace(string input)
        {
            return Regex.Replace(input, @"\s+", String.Empty);
        }

        public static PrincipalInfo GetPrincipalInfo(string username, params string[] roles)
        {
            var identity = new GenericPrincipal(
                new GenericIdentity(username),
                roles);

            return new PrincipalInfo(identity);
        }
    }
}
