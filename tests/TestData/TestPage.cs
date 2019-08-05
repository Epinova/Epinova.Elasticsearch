using System;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Models.Properties;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Forms.Core;

namespace TestData
{
    public class TestPageInherited : TestPage
    {
        public virtual string TestProp2 { get; set; }
    }

    public class TestPageXhtmlString : TestPage
    {
        [Searchable]
        public virtual XhtmlString XhtmlString { get; set; }
    }

    public class TestPage : PageData, ITestPage, IFileUploadElementBlock
    {
        public virtual TestBlock LocalBlock { get; set; }
        [Searchable]
        public virtual string TestProp { get; set; }
        [Searchable]
        [Stem]
        public virtual string StemmedProp { get; set; }
        [Searchable]
        public virtual int TestIntProp { get; set; }
        [Searchable]
        public virtual double TestDoubleProp { get; set; }
        [Searchable]
        public virtual decimal TestDecimalProp { get; set; }
        [Searchable]
        public virtual int? TestIntNullableProp { get; set; }
        [Searchable]
        public virtual DateTime TestDateProp { get; set; }
        [Searchable]
        public virtual DateTime? TestDateNullableProp { get; set; }
        [Searchable]
        public virtual int[] Path { get; set; }
        [Searchable]
        public override DateTime? StartPublish { get; set; }

        public IntegerRange TestIntegerRange { get; set; }
    }

    public interface ITestPage : IContent
    {
        string TestProp { get; set; }
    }

    public static class TestPageExtensions
    {
        public static string CustomStuff(this TestPage page) => Factory.GetSentence();

        public static int Prize(this TestPage page) => Factory.GetInteger();
    }
}
