using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Utilities;
using NUnit.Framework;

namespace Integration.Tests
{
    [TestFixture]
    public class MappingTests
    {
        [Test]
        public void GetIndexMapping_ReturnsMinimumOfMappings()
        {
            IndexMapping result = Mapping.GetIndexMapping(typeof(IndexItem), null, null);

            Assert.AreEqual("snowball", result.All.Analyzer);
            Assert.True(result.Properties.Count > 2);
        }


        [Test]
        public void GetIndexMapping_ReturnsLangMapping()
        {
            IndexMapping result = Mapping.GetIndexMapping(typeof(IndexItem), null, null);
            IndexMappingProperty mapping = result.Properties.Single(p => p.Key == DefaultFields.Lang).Value;

            Assert.AreEqual(MappingPatterns.StringType, mapping.Type);
        }


        [Test]
        public void GetIndexMapping_ReturnsAttachmentMapping()
        {
            IndexMapping result = Mapping.GetIndexMapping(typeof(IndexItem), null, null);
            IndexMappingProperty mapping = result.Properties.Single(p => p.Key == DefaultFields.Attachment).Value;

            Assert.AreEqual(MappingPatterns.StringType, mapping.Type);
            Assert.AreEqual(MappingPatterns.StringType, mapping.Fields.Content.Type);
            Assert.AreEqual("with_positions_offsets", mapping.Fields.Content.TermVector);
            Assert.True(mapping.Fields.Content.Store);
        }


        [Test]
        public void GetIndexMapping_ReturnsSuggestMapping()
        {
            IndexMapping result = Mapping.GetIndexMapping(typeof(IndexItem), null, null);
            IndexMappingProperty mapping = result.Properties.Single(p => p.Key == DefaultFields.Suggest).Value;

            Assert.AreEqual("completion", mapping.Type);
            Assert.AreEqual("suggest", mapping.Analyzer);
        }
    }
}