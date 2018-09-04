using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using NUnit.Framework;
using TestData;
using Indexing = Epinova.ElasticSearch.Core.Conventions.Indexing;

namespace Integration.Tests
{
    [TestFixture]
    public class IndexContentTests
    {
        private IElasticSearchService _service;
        private IIndexer _indexer;
        private IElasticSearchSettings _settings;
        private CultureInfo _defaultCulture;

        private static object[] _nullValues =
        {
            new object[] { "Field1", null },
            new object[] { "Field2", (string)null },
            new object[] { "Field3", (int?)null },
            new object[] { "Field4", (bool?)null },
            new object[] { "Field5", (double?)null },
            new object[] { "Field6", (float?)null }
        };

        [SetUp]
        public void SetUp()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
            _indexer = ServiceLocator.Current.GetInstance<IIndexer>();
            _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            Indexing.Roots.Clear();
            Indexing.CustomProperties.Clear();
            _defaultCulture = CultureInfo.CurrentCulture;
        }

        [TearDown]
        public void Teardown()
        {
            Indexing.Roots.Clear();
            Indexing.CustomProperties.Clear();
            CultureInfo.CurrentCulture = _defaultCulture;
        }


        [Test]
        public void Update_ParentIdExcludedByRootDoesNotAddToIndex()
        {
            int pageId = Factory.GetInteger();
            int parentId = Factory.GetInteger();
            Indexing.Instance.ExcludeRoot(parentId);

            TestPage page = new TestPage();
            page.Property["PageLink"] = new PropertyPageReference(pageId);
            page.Property["PageParentLink"] = new PropertyPageReference(parentId);
            page.Property["PageShortcutType"] = new PropertyNumber((int)PageShortcutType.Normal);

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<TestPage>().GetResults();

            Assert.AreEqual(IndexingStatus.ExcludedByConvention, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Test]
        public void Update_OwnIdExcludedByRootDoesNotAddToIndex()
        {
            int pageId = Factory.GetInteger();
            int parentId = Factory.GetInteger();
            Indexing.Instance.ExcludeRoot(pageId);

            TestPage page = new TestPage();
            page.Property["PageLink"] = new PropertyPageReference(pageId);
            page.Property["PageParentLink"] = new PropertyPageReference(parentId);
            page.Property["PageShortcutType"] = new PropertyNumber((int)PageShortcutType.Normal);

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<TestPage>().GetResults();

            Assert.AreEqual(IndexingStatus.ExcludedByConvention, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Test]
        public void Update_AncestorExcludedByRootDoesNotAddToIndex()
        {
            int pageId = Factory.GetInteger();
            int parentId = Factory.GetInteger();
            int ancestorId = Factory.GetInteger();

            TestPage page = Factory.GetTestPage(pageId, parentId);

            // Not using IntegrationTestHelper here since we need access to SetContentPathGetter
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexer = new Indexer(new CoreIndexer(settings), settings, null);

            Indexing.Instance.ExcludeRoot(ancestorId);
            //Issues with shared statics in parallel runs. Func-ing it in 
            indexer.SetContentPathGetter(x => new[] { ancestorId, parentId, pageId });

            IndexingStatus status = indexer.Update(page);

            SearchResult result = _service.Get<TestPage>().GetResults();

            Assert.AreEqual(IndexingStatus.ExcludedByConvention, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Test]
        public void Update_AddsContentToIndex()
        {
            PageData page = Factory.GetPageData();


            var indexer = new Indexer(new CoreIndexer(_settings), _settings, null);
            indexer.Update(page);

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<PageData>().GetResults();

            Assert.AreEqual(IndexingStatus.Ok, status);
            Assert.True(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [TestCase("HelloWorld", "docx")]
        [TestCase("HelloWorld", "pdf")]
        public void Update_AddsAllowedFilesToIndex(string name, string ext)
        {
            var file = Factory.GetMediaData(name, ext);

            IndexingStatus status = IntegrationTestHelper.Update(file);

            SearchResult result = _service.Get<TestMedia>().GetResults();

            Assert.AreEqual(IndexingStatus.Ok, status);
            Assert.True(result.Hits.Any(h => h.Id == file.ContentLink.ID));
        }


        [Test]
        public void Update_DoesNotAddDisallowedFilesToIndex()
        {
            var file = Factory.GetMediaData("HelloWorld", "exe");

            _indexer.Update(file);

            SearchResult result = _service.Get<TestMedia>().GetResults();

            Assert.False(result.Hits.Any(h => h.Id == file.ContentLink.ID));
        }


        [Test]
        public void Update_ExcludedTypeIsNotAddedToIndex()
        {
            Indexing.Instance.ExcludeType(typeof(TypeWithoutBoosting));

            TypeWithoutBoosting page = new TypeWithoutBoosting();

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<TypeWithoutBoosting>().GetResults();

            Assert.AreEqual(IndexingStatus.ExcludedByConvention, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Test]
        public void Update_LocalBlockIsIndexed()
        {
            string propValue = Factory.GetString();

            TestPage page = Factory.GetPageData<TestPage>();
            page.LocalBlock = new TestBlock
            {
                TestProp = propValue
            };

            IntegrationTestHelper.Update(page);

            SearchResult result = _service.Search<TestPage>(propValue).GetResults();
            SearchHit searchHit = result.Hits.FirstOrDefault(h => h.Id == page.ContentLink.ID);
            Assert.NotNull(searchHit);
        }


        [Test]
        public void Update_LocalNestedBlockIsIndexed()
        {
            string propValue = Factory.GetString();

            TestPage page = Factory.GetPageData<TestPage>();
            page.LocalBlock = new TestBlock
            {
                SubBlock = new TestBlockInherited
                {
                    TestProp = propValue
                }
            };

            IntegrationTestHelper.Update(page);

            SearchResult result = _service.Search<TestPage>(propValue).GetResults();
            SearchHit searchHit = result.Hits.FirstOrDefault(h => h.Id == page.ContentLink.ID);
            Assert.NotNull(searchHit);
        }


        [Test]
        public void Update_ContentWithHideFromSearchIsNotAddedToIndex()
        {
            int id = Factory.GetInteger();
            TypeWithHideFromSearchProperty page = new TypeWithHideFromSearchProperty
            {
                ContentLink = new ContentReference(id)
            };

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<PageData>().GetResults();

            Assert.AreEqual(IndexingStatus.HideFromSearchProperty, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [TestCase(PageShortcutType.FetchData)]
        [TestCase(PageShortcutType.External)]
        [TestCase(PageShortcutType.Inactive)]
        [TestCase(PageShortcutType.Shortcut)]
        public void Update_PageWithShortcutIsNotAddedToIndex(PageShortcutType shortcutType)
        {
            TestPage page = Factory.GetPageData<TestPage>();
            page.LinkType = shortcutType;

            IndexingStatus status = IntegrationTestHelper.Update(page);

            SearchResult result = _service.Get<TestPage>().GetResults();

            Assert.AreEqual(IndexingStatus.HideFromSearchProperty, status);
            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Ignore("Only fails in CI")]
        [Test]
        public void Update_PageChangedToShortcutIsRemovedFromIndex()
        {
            // Index a normal page
            TestPage page = Factory.GetPageData<TestPage>(id: 42);
            IndexingStatus updateStatus = _indexer.Update(page);
            Console.WriteLine("Status 1: {0}", updateStatus);
            var index = new Index(_settings, ElasticFixtureSettings.IndexName);
            bool waitStatus = index.WaitForStatus(20, "green");
            Console.WriteLine("Wait 1: {0}", waitStatus);

            // Check for hit
            SearchResult result = _service.Get<TestPage>().GetResults();
            Assert.True(result.Hits.Any(h => h.Id == page.ContentLink.ID));

            // Change link type to shortcut and re-index
            page.LinkType = PageShortcutType.Shortcut;
            updateStatus = _indexer.Update(page);
            Console.WriteLine("Status 2: {0}", updateStatus);
            waitStatus = index.WaitForStatus(20, "green");
            Console.WriteLine("Wait 2: {0}", waitStatus);

            // Check for no hits
            result = _service.Get<TestPage>().GetResults();

            Console.WriteLine("Hits: {0}", result.Hits.Count());

            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }

        
        [Test]
        public void Update_MediaIsAddedToIndex()
        {
            TestMedia media = Factory.GetMediaData("HelloWorld", "pdf");

            IndexingStatus status = IntegrationTestHelper.Update(media);

            SearchResult result = _service.Get<TestMedia>().GetResults();

            Assert.AreEqual(IndexingStatus.Ok, status);
            Assert.True(result.Hits.Any(h => h.Id == media.ContentLink.ID));
        }


        [Test]
        public void Update_CustomFieldsFromExtensionIsIndexed()
        {
            Indexing.Instance
                .ForType<TestPage>().IncludeField(x => x.CustomStuff());

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.NotNull(hit.CustomProperties["CustomStuff"]);
        }


        [Test]
        [TestCase("Bool1", false)]
        [TestCase("Bool2", true)]
        public void Update_CustomBoolFieldsIsIndexed(string name, bool value)
        {
            Indexing.Instance
                .ForType<TestPage>().IncludeField(name, x => value);

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.AreEqual(value.ToString(), hit.CustomProperties[name].ToString());
        }


        [Test]
        [TestCaseSource(nameof(_nullValues))]
        public void Update_CustomFieldsWithNullValueIsNotIndexed(string name, object value)
        {
            Indexing.Instance
                .ForType<TestPage>().IncludeField(name, x => value);

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.False(hit.CustomProperties.ContainsKey(name));
        }
        

        [Test]
        [TestCase("Num1", 42)]
        [TestCase("Num2", 42.1d)]
        [TestCase("Num3", 42.2f)]
        [TestCase("Num4", -42.5f)]
        //[TestCase("Num3", 42.4m)] // NUnit struggles with decimal
        public void Update_CustomNumericFieldsIsIndexed(string name, object value)
        {
            // Ensure "." is used as comma separator
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Indexing.Instance
                .ForType<TestPage>().IncludeField(name, x => value);

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.AreEqual(value.ToString(), hit.CustomProperties[name].ToString());
        }


        [Test]
        [TestCase("Arr1", new[] {1, 2, 3})]
        [TestCase("Arr2", new[] {"a", "b", "c"})]
        public void Update_CustomIenumerableFieldsIsIndexed(string name, IEnumerable value)
        {
            Indexing.Instance
                .ForType<TestPage>().IncludeField(name, x => value);

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.AreEqual(value, hit.CustomProperties[name]);
        }


        [Test]
        public void Get_ReturnsCustomFields()
        {
            Indexing.Instance
                .ForType<TestPage>().IncludeField("Num1", x => 42.1m)
                .ForType<TestPage>().IncludeField("Arr1", x => new[] { 1, 2, 3 })
                .ForType<TestPage>().IncludeField(x => x.CustomStuff());

            TestPage page1 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Get<TestPage>()
                .GetResults();

            SearchHit hit = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID);

            Assert.AreEqual(42.1m, Convert.ToDecimal(hit.CustomProperties["Num1"]));
            Assert.NotNull(hit.CustomProperties["CustomStuff"]);
            Assert.NotNull(hit.CustomProperties["Arr1"]);
        }


        [Test]
        public void Update_CustomFieldsInBaseClassIsIndexed()
        {
            Indexing.Instance
                .ForType<TestPageInherited>().IncludeField(x => x.TestProp)
                .ForType<TestPageInherited>().IncludeField(x => x.TestProp2);

            TestPageInherited page1 = Factory.GetPageData<TestPageInherited>();
            page1.TestProp = "page 1 prop";
            page1.TestProp2 = "page 1 prop2";

            IntegrationTestHelper.Update(page1);

            SearchResult searchResult = _service.Search<TestPageInherited>("page")
                .GetResults();

            string result1 = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID).CustomProperties["TestProp"] as string;
            string result2 = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID).CustomProperties["TestProp2"] as string;

            Assert.IsNotEmpty(result1);
            Assert.IsNotEmpty(result2);
        }


        [Ignore("Only breaks in CI. Find out why.")]
        [Test]
        public void Update_CustomFieldsFromInterfaceIsIndexed()
        {
            Indexing.Instance.ForType<ITestPage>().IncludeField(x => x.TestProp);

            ITestPage page1 = Factory.GetPageData<TestPage>();
            ITestPage page2 = Factory.GetPageData<TestPageInherited>();
            page1.TestProp = "page 1 prop";
            page2.TestProp = "page 2 prop";

            var index = new Index(_settings, ElasticFixtureSettings.IndexName);

            IndexingStatus updateStatus = _indexer.Update(page1);
            Console.WriteLine("Status 1: {0}", updateStatus);
            bool waitStatus = index.WaitForStatus(20, "green");
            Console.WriteLine("Wait 1: {0}", waitStatus);

            updateStatus = _indexer.Update(page2);
            Console.WriteLine("Status 2: {0}", updateStatus);
            waitStatus = index.WaitForStatus(20, "green");
            Console.WriteLine("Wait 2: {0}", waitStatus);

            SearchResult searchResult = _service.Get<ITestPage>()
                .GetResults();

            Console.WriteLine("Hits: {0}", searchResult.Hits.Count());

            string result1 = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID).CustomProperties["TestProp"] as string;
            string result2 = searchResult.Hits.First(x => x.Id == page2.ContentLink.ID).CustomProperties["TestProp"] as string;

            Assert.IsNotEmpty(result1);
            Assert.IsNotEmpty(result2);
        }
    }
}