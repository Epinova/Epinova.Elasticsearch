﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Moq;
using Xunit;

namespace Core.Tests.Settings.Configuration
{
    public class ElasticsearchSectionTests
    {
        public ElasticsearchSectionTests()
        {
            var mockSection = new Mock<ElasticSearchSection>();

            mockSection
                .SetupGet(m => m.Indices)
                .Returns(new IndicesCollection());

            mockSection
                .SetupGet(m => m.Files)
                .Returns(new FilesCollection());

            _section = mockSection.Object;
        }

        private readonly ElasticSearchSection _section;


        [Theory]
        [InlineData("1")]
        [InlineData("42")]
        [InlineData("1024")]
        [InlineData("102400")]
        [InlineData(Int64.MaxValue)]
        [InlineData("42KB")]
        [InlineData("42MB")]
        [InlineData("42GB")]
        public void IsValidSizeString_ValidString_ReturnsTrue(string size)
        {
            var result = _section.IsValidSizeString(size);

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-42")]
        [InlineData(Int64.MinValue)]
        [InlineData("42XB")]
        [InlineData("42XXXX")]
        [InlineData("XXXX")]
        [InlineData("GB42")]
        public void IsValidSizeString_InvalidString_ReturnsTrue(string size)
        {
            var result = _section.IsValidSizeString(size);

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\n")]
        public void ValidateIndices_EmptyName_Throws(string name)
        {
            _section.Indices.Add(new IndexConfiguration { Name = name });

            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateIndices());

            Assert.Equal("Configuration Error. Index name cannot be empty", exception.Message);
        }

        [Theory]
        [MemberData(nameof(GetIndexNameInvalidCharacters))]
        public void ValidateIndices_InvalidCharInName_Throws(char character)
        {
            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.Indices.Add(new IndexConfiguration
                {
                    Name = "foo" + character
                })
            );
            Console.WriteLine(exception.Message);
            Assert.StartsWith("The value for the property 'name' is not", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateFiles_EmptyExtension_Throws(string ext)
        {
            _section.Files.Add(new FileConfiguration
            {
                Extension = ext
            });

            Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateFiles()
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(-99)]
        public void ValidateIndexingMaxDegreeOfParallelism_invalidSetting_Throws(int setting)
        {
            var mockSection = new Mock<ElasticSearchSection>();
            mockSection
                .Setup(m => m.IndexingMaxDegreeOfParallelism)
                .Returns(setting);

            ElasticSearchSection section = mockSection.Object;

            Assert.Throws<ConfigurationErrorsException>(() =>
                section.ValidateIndexingMaxDegreeOfParallelism()
            );
        }

        [Theory]
        [MemberData(nameof(GetFileExtensionInvalidCharacters))]
        public void ValidateFiles_InvalidCharInExtension_Throws(char character)
        {
            Assert.Throws<ConfigurationErrorsException>(() =>
                _section.Files.Add(new FileConfiguration
                {
                    Extension = "foo" + character
                })
            );
        }


        private static IEnumerable<object[]> GetIndexNameInvalidCharacters()
        {
            foreach (var c in IndexConfiguration.InvalidCharacters)
                yield return new object[] { c };
        }

        private static IEnumerable<object[]> GetFileExtensionInvalidCharacters()
        {
            foreach (var c in FileConfiguration.InvalidCharacters)
                yield return new object[] { c };
        }

        [Fact]
        public void ValidateFiles_NoFiles_IsOk()
        {
            _section.Files.Clear();
            _section.ValidateFiles();
        }

        [Fact]
        public void ValidateIndices_MissingTypeOnCustomIndex_Throws()
        {
            _section.Indices.Add(new IndexConfiguration { Name = "foo", Default = true });
            _section.Indices.Add(new IndexConfiguration { Name = "bar" });

            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateIndices());

            Assert.Equal("Configuration Error. Custom indices must define a type", exception.Message);
        }

        [Fact]
        public void ValidateIndices_MultipleDefaultIndices_Throws()
        {
            _section.Indices.Add(new IndexConfiguration { Name = "foo", Default = true });
            _section.Indices.Add(new IndexConfiguration { Name = "bar", Default = true });

            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateIndices());

            Assert.Equal("Configuration Error. Only one index can be set as default", exception.Message);
        }

        [Fact]
        public void ValidateIndices_NoDefaultIndex_Throws()
        {
            _section.Indices.Add(new IndexConfiguration { Name = "foo" });
            _section.Indices.Add(new IndexConfiguration { Name = "bar" });

            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateIndices());

            Assert.Equal("Configuration Error. One index must be set as default when adding multiple indices",
                exception.Message);
        }

        [Fact]
        public void ValidateIndices_NoIndices_Throws()
        {
            var exception = Assert.Throws<ConfigurationErrorsException>(() =>
                _section.ValidateIndices());

            Assert.Equal("Configuration Error. You must add at least one index to the <indices> node",
                exception.Message);
        }
    }
}