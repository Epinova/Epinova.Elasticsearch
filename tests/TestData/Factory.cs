using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.DataAccess.Internal;
using EPiServer.Framework.Blobs;
using EPiServer.Framework.Web;
using EPiServer.Scheduler;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Moq;

namespace TestData
{
    public static class Factory
    {
        private static readonly Random _random = new Random();

        internal static ServiceLocationMock SetupServiceLocator()
        {
            var result = new ServiceLocationMock
            {
                ServiceLocatorMock = new Mock<IServiceLocator>(),
                StateAssesorMock = new Mock<IPublishedStateAssessor>(),
                ScheduledJobRepositoryMock = new Mock<IScheduledJobRepository>(),
                ScheduledJobExecutorMock = new Mock<IScheduledJobExecutor>(),
                TemplateResolver = GetTemplateResolver()
            };

            result.StateAssesorMock = new Mock<IPublishedStateAssessor>();
            result.StateAssesorMock
                .Setup(m => m.IsPublished(It.IsAny<IContent>(), It.IsAny<PublishedStateCondition>()))
                .Returns(true);

            var pathLinks = new[]
            {
                GetPageReference(),
                GetPageReference(),
                GetPageReference()
            };
            var dbExecMock = new Mock<IDatabaseExecutor>();
            dbExecMock
                .Setup(m => m.Execute(It.IsAny<Func<ContentPath>>()))
                .Returns(new ContentPath(pathLinks));

            var contentPathMock = new Mock<ContentPathDB>(dbExecMock.Object);
            var bestbetMock = new Mock<IBestBetsRepository>();
            var boostMock = new Mock<IBoostingRepository>();
            boostMock
                .Setup(m => m.GetByType(It.IsAny<Type>()))
                .Returns(new Dictionary<string, int>());

            var languageMock = new Mock<ILanguageBranchRepository>();
            languageMock.Setup(m => m.ListEnabled()).Returns(new List<LanguageBranch>
            {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
            });
            result.LanguageBranchRepositoryMock = languageMock;

            var settings = new Mock<IElasticSearchSettings>();
            settings.Setup(m => m.BulkSize).Returns(1000);
            settings.Setup(m => m.CloseIndexDelay).Returns(2000);
            settings.Setup(m => m.EnableFileIndexing).Returns(true);
            settings.Setup(m => m.IgnoreXhtmlStringContentFragments).Returns(false);
            settings.Setup(m => m.Index).Returns(ElasticFixtureSettings.IndexName);
            settings.Setup(m => m.Indices).Returns(new[] { ElasticFixtureSettings.IndexNameWithoutLang });
            settings.Setup(m => m.GetLanguage(It.IsAny<string>())).Returns("no");
            settings.Setup(m => m.GetDefaultIndexName(It.IsAny<string>()))
                .Returns(ElasticFixtureSettings.IndexName);
            settings.Setup(m => m.Host).Returns("http://example.com");
            result.SettingsMock = settings;

            var httpClient = new Mock<IHttpClientHelper>();
            httpClient.Setup(m => m.Delete(It.IsAny<Uri>())).Returns(true);
            httpClient.Setup(m => m.Head(It.IsAny<Uri>())).Returns(HttpStatusCode.OK);

            result.HttpClientMock = httpClient;

            var serverInfo = new Mock<IServerInfoService>();
            serverInfo.Setup(m => m.GetInfo())
                .Returns(new ServerInfo
                {
                    ElasticVersion = new ServerInfo.InternalVersion
                    {
                        Number = "6.0.0",
                        LuceneVersion = "7.0.1"
                    }
                });

            result.ServerInfoMock = serverInfo;

            var synonymRepositoryMock = new Mock<ISynonymRepository>();
            synonymRepositoryMock
                .Setup(m => m.GetSynonyms(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<Synonym>());
            result.SynonymRepositoryMock = synonymRepositoryMock;

            result.IndexerMock = new Mock<IIndexer>();
            result.CoreIndexerMock = new Mock<ICoreIndexer>();
            result.ContentLoaderMock = new Mock<IContentLoader>();
            result.ContentIndexServiceMock = new Mock<IContentIndexService>();
            result.ServiceMock = new Mock<IElasticSearchService<IContent>>();

            result.ServiceLocatorMock.Setup(m => m.GetInstance<IHttpClientHelper>()).Returns(httpClient.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ILanguageBranchRepository>()).Returns(languageMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IContentLoader>()).Returns(result.ContentLoaderMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IIndexer>()).Returns(result.IndexerMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ICoreIndexer>()).Returns(result.CoreIndexerMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IContentVersionRepository>()).Returns(new Mock<IContentVersionRepository>().Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IBoostingRepository>()).Returns(boostMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IBestBetsRepository>()).Returns(bestbetMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IServerInfoService>()).Returns(serverInfo.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IElasticSearchSettings>()).Returns(settings.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IElasticSearchService>()).Returns(new ElasticSearchService(serverInfo.Object, settings.Object, httpClient.Object));
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IPublishedStateAssessor>()).Returns(result.StateAssesorMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ITemplateResolver>()).Returns(result.TemplateResolver);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ContentPathDB>()).Returns(contentPathMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IScheduledJobRepository>()).Returns(result.ScheduledJobRepositoryMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<IScheduledJobExecutor>()).Returns(result.ScheduledJobExecutorMock.Object);
            result.ServiceLocatorMock.Setup(m => m.GetInstance<ContentAssetHelper>()).Returns(new Mock<ContentAssetHelper>().Object);

            ServiceLocator.SetLocator(result.ServiceLocatorMock.Object);
            return result;
        }

        public static string GetString(int size = 20)
        {
            var instance = Guid.NewGuid().ToString("N");

            while(instance.Length < size)
            {
                instance += Guid.NewGuid().ToString("N");
            }

            return instance.Substring(0, size);
        }

        public static string GetSentence(int words = 5)
        {
            string instance = String.Empty;

            for(var i = 0; i < words; i++)
            {
                instance += GetString(_random.Next(3, 8)) + " ";
            }

            return instance.Trim();
        }

        public static PageData GetPageData(
            bool visibleInMenu = true,
            bool isPublished = true,
            bool userHasAccess = true,
            bool isNotInWaste = true,
            PageShortcutType shortcutType = PageShortcutType.Normal,
            int id = 0,
            int parentId = 0) => GetPageData<PageData>(visibleInMenu, isPublished, userHasAccess, isNotInWaste, shortcutType, id, parentId);

        public static T GetPageData<T>(
            bool visibleInMenu = true,
            bool isPublished = true,
            bool userHasAccess = true,
            bool isNotInWaste = true,
            PageShortcutType shortcutType = PageShortcutType.Normal,
            int id = 0,
            int parentId = 0,
            CultureInfo language = null) where T : PageData
        {
            var securityDescriptor = new Mock<IContentSecurityDescriptor>();
            securityDescriptor.Setup(m => m.HasAccess(It.IsAny<IPrincipal>(), It.IsAny<AccessLevel>())).Returns(userHasAccess);

            if(language == null)
            {
                language = CultureInfo.CurrentCulture;
            }

            PageReference pageLink = id > 0 ? new PageReference(id) : GetPageReference();
            PageReference parentLink = parentId > 0 ? new PageReference(parentId) : GetPageReference();

            var pageGuid = Guid.NewGuid();
            var instance = new Mock<T>();
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
            if(!isNotInWaste)
            {
                instance.Setup(m => m.ContentLink).Returns(ContentReference.WasteBasket);
            }

            instance.Object.PageTypeName = typeof(T).Name;
            instance.Object.LinkType = shortcutType;
            return instance.Object;
        }

        public static PageReference GetPageReference() => new PageReference(GetInteger(1000));

        public static int GetInteger(int start = 1, int count = 1337) => Enumerable.Range(start, count).OrderBy(_ => Guid.NewGuid()).First();

        public static TemplateResolver GetTemplateResolver(bool hasTemplate = true)
        {
            var templateResolver = new Mock<TemplateResolver>();

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

            if(!File.Exists(path))
            {
                return String.Empty;
            }

            return File.ReadAllText(path);
        }

        public static TestMedia GetMediaData(string name, string ext)
        {
            PageReference pageLink = GetPageReference();
            PageReference parentLink = GetPageReference();
            var pageGuid = Guid.NewGuid();
            var blob = new FileBlob(new Uri("foo://bar.com/"), GetFilePath("Media", name + "." + ext));

            var instance = new Mock<TestMedia>();
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
            if(s == null)
            {
                return null;
            }

            var fragmentMock = new Mock<IStringFragment>();
            fragmentMock.SetupAllProperties();
            fragmentMock.Setup(m => m.ToString()).Returns(s);
            fragmentMock.Setup(m => m.GetViewFormat()).Returns(s);

            var xhtmlStringMock = new Mock<XhtmlString>();
            var fragments = new StringFragmentCollection();
            if(!String.IsNullOrWhiteSpace(s))
            {
                fragments.Add(fragmentMock.Object);
            }

            if(additionalFragments?.Length > 0)
            {
                foreach(IStringFragment fragment in additionalFragments)
                {
                    fragments.Add(fragment);
                }
            }

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

        public static bool ArrayEquals<T1, T2>(IEnumerable<T1> arr1, IEnumerable<T2> arr2)
        {
            var obj1 = arr1.Select(x => x as object);
            var obj2 = arr2.Select(x => x as object);
            return obj1.SequenceEqual(obj2);
        }

        public static string RemoveWhitespace(string input) => Regex.Replace(input, @"\s+", String.Empty);

        public static (IContent Content, SaveContentEventArgs Args) GetPublishScenario()
        {
            var page = GetPageData();

            return (page, new SaveContentEventArgs(
                page.ContentLink,
                page,
                SaveAction.Publish,
                new StatusTransition(
                    VersionStatus.CheckedOut,
                    VersionStatus.Published,
                    false
                )));
        }

        public static (IContent Content, DeleteContentEventArgs Args) GetDeleteScenario()
        {
            var page = GetPageData();
            var args = new DeleteContentEventArgs(page.ContentLink, ContentReference.WasteBasket)
            {
                Content = page
            };

            return (page, args);
        }

        public static (IContent Content, MoveContentEventArgs Args) GetMoveScenario(params ContentReference[] descendents)
        {
            var page = GetPageData();
            var target = GetPageData();
            var args = new MoveContentEventArgs(page.ContentLink, target.ContentLink)
            {
                Content = page,
                Descendents = descendents
            };

            return (page, args);
        }

        public static (IContent Content, MoveContentEventArgs Args) GetMoveToWasteBasketScenario()
        {
            var page = GetPageData();
            var args = new MoveContentEventArgs(page.ContentLink, ContentReference.WasteBasket)
            {
                Content = page
            };

            return (page, args);
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
