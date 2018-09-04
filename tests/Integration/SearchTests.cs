using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;



//TODO
// Nullable Range (datetime)
//_service.Filter
//_service.Fuzzy
//_service.NoBoosting
//_service.WildcardSearch


namespace Integration.Tests
{
    [TestFixture]
    public class SearchTests
    {
        private IElasticSearchService _service;


        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
        }



        [Test]
        public void Unpublished_Content_IsFiltered()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();

            page1.TestProp = titleKey + " 1";

            page2.TestProp = titleKey + " 2";
            //page2.StopPublish = new DateTime(2000, 1, 1);
            page2.Property["PageStopPublish"] = new PropertyDate(new DateTime(2000, 1, 1));

            IntegrationTestHelper.UpdateMany(page1, page2);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .GetResults();

            int actual = result.Hits.Count();
            Assert.AreEqual(1, actual);
        }


        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Size_RespectsSize(int size)
        {
            string titleKey = Factory.GetString(40) + size;

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Size(size)
                .GetResults();

            int actual = result.Hits.Count();
            Assert.AreEqual(size, actual);
        }



        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void From_SkipsExpectedNumberOfHits(int from)
        {
            string titleKey = Factory.GetString(40) + from;

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .From(from)
                .GetResults();

            int expected = 5 - from;
            int actual = result.Hits.Count();

            Assert.AreEqual(expected, actual);
        }


        [Test]
        public void Exclude_FiltersAwayUnwantedType()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPageInherited>();
            TestPage page5 = Factory.GetPageData<TestPageInherited>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            // Verify that we get 5 hits without exclude
            SearchResult result = _service.Search<TestPage>(titleKey).GetResults();
            Assert.AreEqual(5, result.TotalHits);

            // Verify that we get only 3 hits wiht exclude
            result = _service.Search<TestPage>(titleKey)
                .Exclude<TestPageInherited>()
                .GetResults();

            Assert.AreEqual(3, result.TotalHits);
        }


        [Test]
        public void Range_IntRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestIntProp = 1;
            page2.TestIntProp = 2;
            page3.TestIntProp = 3;
            page4.TestIntProp = 4;
            page5.TestIntProp = 5;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestIntProp, 2, 5)
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_DecimalRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDecimalProp = 1;
            page2.TestDecimalProp = 2;
            page3.TestDecimalProp = 3;
            page4.TestDecimalProp = 4;
            page5.TestDecimalProp = 5;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestDecimalProp, 2, 5)
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_DoubleRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDoubleProp = 1;
            page2.TestDoubleProp = 2;
            page3.TestDoubleProp = 3;
            page4.TestDoubleProp = 4;
            page5.TestDoubleProp = 5;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestDoubleProp, 2, 5)
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_NumericRangeForNullablePropertyReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestIntNullableProp = 1;
            page2.TestIntNullableProp = 2;
            page3.TestIntNullableProp = 3;
            page4.TestIntNullableProp = 4;
            page5.TestIntNullableProp = 5;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestIntNullableProp, 2, 5)
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_NumericRangeForNullablePropertyWithNullValueReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestIntNullableProp = null;
            page2.TestIntNullableProp = null;
            page3.TestIntNullableProp = 3;
            page4.TestIntNullableProp = null;
            page5.TestIntNullableProp = null;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestIntNullableProp, 2, 5)
                .GetResults();

            Assert.AreEqual(1, result.TotalHits);
        }


        [Test]
        public void RangeInclusive_NumericRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestIntProp = 1;
            page2.TestIntProp = 2;
            page3.TestIntProp = 3;
            page4.TestIntProp = 4;
            page5.TestIntProp = 5;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .RangeInclusive(x => x.TestIntProp, 2, 5)
                .GetResults();

            Assert.AreEqual(4, result.TotalHits);
        }


        [Test]
        public void Range_DateRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDateProp = new DateTime(2000, 1, 1);
            page2.TestDateProp = new DateTime(2000, 1, 2);
            page3.TestDateProp = new DateTime(2000, 1, 3);
            page4.TestDateProp = new DateTime(2000, 1, 4);
            page5.TestDateProp = new DateTime(2000, 1, 5);

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestDateProp, new DateTime(2000, 1, 2), new DateTime(2000, 1, 5))
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_DateRangeForNullablePropertyReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDateNullableProp = new DateTime(2000, 1, 1);
            page2.TestDateNullableProp = new DateTime(2000, 1, 2);
            page3.TestDateNullableProp = new DateTime(2000, 1, 3);
            page4.TestDateNullableProp = new DateTime(2000, 1, 4);
            page5.TestDateNullableProp = new DateTime(2000, 1, 5);

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestDateNullableProp, new DateTime(2000, 1, 2), new DateTime(2000, 1, 5))
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }


        [Test]
        public void Range_DateRangeForNullablePropertyWithNullValueReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDateNullableProp = null;
            page2.TestDateNullableProp = null;
            page3.TestDateNullableProp = new DateTime(2000, 1, 3);
            page4.TestDateNullableProp = new DateTime(2000, 1, 4);
            page5.TestDateNullableProp = null;

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .Range(x => x.TestDateNullableProp, new DateTime(2000, 1, 2), new DateTime(2000, 1, 5))
                .GetResults();

            Assert.AreEqual(2, result.TotalHits);
        }



        [Test]
        public void RangeInclusive_DateRangeReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.TestDateProp = new DateTime(2000, 1, 1);
            page2.TestDateProp = new DateTime(2000, 1, 2);
            page3.TestDateProp = new DateTime(2000, 1, 3);
            page4.TestDateProp = new DateTime(2000, 1, 4);
            page5.TestDateProp = new DateTime(2000, 1, 5);

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .RangeInclusive(x => x.TestDateProp, new DateTime(2000, 1, 2), new DateTime(2000, 1, 5))
                .GetResults();

            Assert.AreEqual(4, result.TotalHits);
        }


        
        [TestCase(1, 5)]
        [TestCase(2, 4)]
        [TestCase(3, 3)]
        [TestCase(4, 2)]
        [TestCase(5, 1)]
        public void StartFrom_ReturnsExpectedHits(int startFrom, int expected)
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            TestPage page3 = Factory.GetPageData<TestPage>();
            TestPage page4 = Factory.GetPageData<TestPage>();
            TestPage page5 = Factory.GetPageData<TestPage>();
            page1.TestProp = titleKey + " 1";
            page2.TestProp = titleKey + " 2";
            page3.TestProp = titleKey + " 3";
            page4.TestProp = titleKey + " 4";
            page5.TestProp = titleKey + " 5";
            page1.Path = new[] { 1 };
            page2.Path = new[] { 1, 2 };
            page3.Path = new[] { 1, 2, 3 };
            page4.Path = new[] { 1, 2, 3, 4 };
            page5.Path = new[] { 1, 2, 3, 4, 5 };

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .StartFrom(startFrom)
                .GetResults();

            Assert.AreEqual(expected, result.TotalHits);
        }


        [Test]
        public void InField_ReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPageInherited page1 = Factory.GetPageData<TestPageInherited>();
            TestPageInherited page2 = Factory.GetPageData<TestPageInherited>();
            TestPageInherited page3 = Factory.GetPageData<TestPageInherited>();
            TestPageInherited page4 = Factory.GetPageData<TestPageInherited>();
            TestPageInherited page5 = Factory.GetPageData<TestPageInherited>();
            page1.TestProp = titleKey + " foo";
            page2.TestProp = titleKey + " foo";
            page3.TestProp2 = titleKey + " bar";
            page4.TestProp2 = titleKey + " bar";
            page5.TestProp2 = titleKey + " bar";

            IntegrationTestHelper.UpdateMany(page1, page2, page3, page4, page5);

            // Expect 2 hits in TestProp for "foo"
            SearchResult result = _service.Search<TestPageInherited>(titleKey + " foo", Operator.And)
                .InField(x => x.TestProp)
                .GetResults();
            Assert.AreEqual(2, result.TotalHits);

            // Expect 0 hits in TestProp2 for "foo"
            result = _service.Search<TestPageInherited>(titleKey + " foo", Operator.And)
                .InField(x => x.TestProp2)
                .GetResults();
            Assert.AreEqual(0, result.TotalHits);

            // Expect 3 hits in TestProp2 for "bar"
            result = _service.Search<TestPageInherited>(titleKey + " bar", Operator.And)
                .InField(x => x.TestProp2)
                .GetResults();
            Assert.AreEqual(3, result.TotalHits);

            // Expect 0 hits in TestProp for "bar"
            result = _service.Search<TestPageInherited>(titleKey + " bar", Operator.And)
                .InField(x => x.TestProp)
                .GetResults();
            Assert.AreEqual(0, result.TotalHits);
        }


        [Test]
        public void Boost_ReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPageInherited page1 = Factory.GetPageData<TestPageInherited>();
            TestPageInherited page2 = Factory.GetPageData<TestPageInherited>();

            page1.Name = "page1";
            page1.TestProp = titleKey;

            page2.Name = "page2";
            page2.TestProp2 = titleKey;

            IntegrationTestHelper.UpdateMany(page1, page2);

            // Boost TestProp, should favour Page 1
            SearchResult result = _service.Search<TestPageInherited>(titleKey)
                .Boost(x => x.TestProp, 42)
                .GetResults();
            SearchHit topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page1.Name, topHit.Name);

            // Boost TestProp2, should favour Page 2
            result = _service.Search<TestPageInherited>(titleKey)
                .Boost(x => x.TestProp2, 42)
                .GetResults();
            topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page2.Name, topHit.Name);
        }


        [Test]
        public void BoostType_ReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPageInherited page2 = Factory.GetPageData<TestPageInherited>();

            page1.Name = "page1";
            page1.TestProp = titleKey;

            page2.Name = "page2";
            page2.TestProp2 = titleKey;

            IntegrationTestHelper.UpdateMany(page1, page2);

            // Boost TestPage, should favour Page 1
            SearchResult result = _service.Search<TestPage>(titleKey)
                .Boost<TestPage>(42)
                .GetResults();

            SearchHit topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page1.Name, topHit.Name);

            // Boost TestPageInherited, should favour Page 2
            result = _service.Search<TestPageInherited>(titleKey)
                .Boost<TestPageInherited>(42)
                .GetResults();

            topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page2.Name, topHit.Name);
        }


        [Test]
        public void BoostByAncestor_ReturnsExpectedHits()
        {
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();

            page1.Name = "page1";
            page1.TestProp = titleKey;
            page1.Path = new[] { 1, 2, 3, 1337 };

            page2.Name = "page2";
            page2.TestProp = titleKey;
            page2.Path = new[] { 1, 2, 3, 42 };

            IntegrationTestHelper.UpdateMany(page1, page2);

            // Boost ancestor 1337, should favour Page 1
            SearchResult result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .BoostByAncestor(1337, 100)
                .GetResults();

            SearchHit topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page1.Name, topHit.Name);

            // Boost ancestor 1337 negative, should favour Page 2
            result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .BoostByAncestor(1337, -100)
                .GetResults();

            topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page2.Name, topHit.Name);

            // Boost ancestor 42, should favour Page 2
            result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .BoostByAncestor(42, 100)
                .GetResults();

            topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page2.Name, topHit.Name);

            // Boost ancestor 42 negative, should favour Page 1
            result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .BoostByAncestor(new ContentReference(42), -100)
                .GetResults();

            topHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            Assert.AreEqual(page1.Name, topHit.Name);
        }


        [Test]
        public void Decay_ReturnsExpectedHits()
        {
            DateTime now = DateTime.Now;
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetTestPage();
            TestPage page2 = Factory.GetTestPage();
            TestPage page3 = Factory.GetTestPage();

            page1.Name = "page1";
            page1.TestProp = titleKey;
            //page1.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-90));
            page1.StartPublish = now.AddDays(-90);

            page2.Name = "page2";
            page2.TestProp = titleKey;
            //page2.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-60));
            page2.StartPublish = now.AddDays(-60);

            page3.Name = "page3";
            page3.TestProp = titleKey;
            //page3.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-3));
            page3.StartPublish = now.AddDays(-3);

            IntegrationTestHelper.UpdateMany(page1, page2, page3);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .Decay(x => x.StartPublish, TimeSpan.FromDays(30))
                .GetResults();

            foreach (var hit in result.Hits)
            {
                Console.WriteLine($"{hit.QueryScore} {hit.Name}");
            }

            var firstHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            var lastHit = result.Hits.OrderByDescending(h => h.QueryScore).Last();

            Assert.AreEqual(page3.Name, firstHit.Name);
            Assert.AreEqual(page1.Name, lastHit.Name);
        }


        [Test]
        public void Decay_ManualFieldName_ReturnsExpectedHits()
        {
            DateTime now = DateTime.Now;
            string titleKey = Factory.GetString(40);

            TestPage page1 = Factory.GetTestPage();
            TestPage page2 = Factory.GetTestPage();
            TestPage page3 = Factory.GetTestPage();

            page1.Name = "page1";
            page1.TestProp = titleKey;
            //page1.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-90));
            page1.StartPublish = now.AddDays(-90);

            page2.Name = "page2";
            page2.TestProp = titleKey;
            //page2.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-60));
            page2.StartPublish = now.AddDays(-60);

            page3.Name = "page3";
            page3.TestProp = titleKey;
            //page3.Property["PageStartPublish"] = new PropertyDate(now.AddDays(-3));
            page3.StartPublish = now.AddDays(-3);

            IntegrationTestHelper.UpdateMany(page1, page2, page3);

            SearchResult result = _service.Search<TestPage>(titleKey)
                .InField(x => x.TestProp)
                .Decay("StartPublish", TimeSpan.FromDays(5))
                .GetResults();

            foreach (var hit in result.Hits)
            {
                Console.WriteLine($"{hit.QueryScore} {hit.Name}");
            }

            var firstHit = result.Hits.OrderByDescending(h => h.QueryScore).First();
            var lastHit = result.Hits.OrderByDescending(h => h.QueryScore).Last();

            Assert.AreEqual(page3.Name, firstHit.Name);
            Assert.AreEqual(page1.Name, lastHit.Name);
        }


        [Test]
        public void ScriptScore_ReturnsExpectedHits()
        {
            TestPage page1 = Factory.GetTestPage();
            TestPage page2 = Factory.GetTestPage();
            TestPage page3 = Factory.GetTestPage();

            page1.Name = "page 1";
            page1.TestIntProp = 1;
            page2.Name = "page 2";
            page2.TestIntProp = 2;
            page3.Name = "page 3";
            page3.TestIntProp = 3;

            IntegrationTestHelper.UpdateMany(page1, page2, page3);

            SearchResult result = _service.Search<TestPage>("page")
                .InField(x => x.Name)
                .CustomScriptScore("_score * (doc['TestIntProp'].value == 2 ? 10000 : 1)")
                .GetResults();

            foreach (var hit in result.Hits)
            {
                Console.WriteLine($"{hit.QueryScore} {hit.Name}");
            }

            var firstHit = result.Hits.OrderByDescending(h => h.QueryScore).First();

            Assert.AreEqual("page 2", firstHit.Name);
        }


        [TestCase("lorem", "ipsum")]
        [TestCase("Smoot", "Bendit")]
        public void WordsWithDots_IsTokenizedWithCharFilter(string part1, string part2)
        {
            TestPage page = Factory.GetTestPage();
            page.StemmedProp = $"{part1}.{part2}";

            IntegrationTestHelper.Update(page);

            // First word
            SearchResult result = _service.Search<TestPage>(part1)
                .InField(x => x.StemmedProp)
                .GetResults();
            Assert.NotZero(result.Hits.Count());

            // Second word
            result = _service.Search<TestPage>(part2)
                .InField(x => x.StemmedProp)
                .GetResults();
            Assert.NotZero(result.Hits.Count());
        }
    }
}
