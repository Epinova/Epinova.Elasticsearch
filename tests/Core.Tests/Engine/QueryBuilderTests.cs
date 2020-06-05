using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Engine;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Query;
using EPiServer.Core;
using Newtonsoft.Json;
using TestData;
using Xunit;
using Xunit.Abstractions;
using static TestData.Factory;

namespace Core.Tests.Engine
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class QueryBuilderTests
    {
        private readonly ITestOutputHelper _console;
        private readonly QueryBuilder _builder;
        private readonly CultureInfo _language;

        public QueryBuilderTests(ITestOutputHelper console)
        {
            _console = console;
            _language = new CultureInfo("en-US");
            _builder = new QueryBuilder(null, null, null);
            _builder.SetMappedFields(new[] { "bar" });

            Epinova.ElasticSearch.Core.Conventions.Indexing.Roots.Clear();
        }

        [Fact]
        public void Search_ExcludeField_AddsMustNotMatch()
        {
            var request = (QueryRequest)_builder.Search(new QuerySetup
            {
                SearchText = "foo",
                ExcludedTypes = new List<Type>
                {
                    typeof(ComplexType)
                }
            });

            string match = request.Query.Bool.Filter
                .Cast<NestedBoolQuery>().First().Bool.MustNot
                .OfType<MatchSimple>()
                .First()
                .Match[DefaultFields.Types]
                .ToString();

            Assert.Equal(typeof(ComplexType).GetTypeName().ToLower(), match);
        }

        [Fact]
        public void Search_ExcludeMultipleFields_AddsMustNotMatches()
        {
            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var request = (QueryRequest)builder.Search(new QuerySetup
            {
                SearchText = "foo",
                ExcludedTypes = new List<Type>
                {
                    typeof(ComplexType),
                    typeof(TypeWithBoosting)
                }
            });

            string[] match = request.Query.Bool.Filter
                .Cast<NestedBoolQuery>().First().Bool.MustNot
                .OfType<MatchSimple>()
                .Select(m => m.Match[DefaultFields.Types]?.ToString())
                .ToArray();

            Assert.True(match.Contains(typeof(ComplexType).GetTypeName().ToLower()));
            Assert.True(match.Contains(typeof(TypeWithBoosting).GetTypeName().ToLower()));
        }

        [Fact]
        public void Search_BoostedFields_AddsShouldMatch()
        {
            const string field = "foo";
            const byte weight = 3;

            var request = (QueryRequest)_builder.Search(new QuerySetup
            {
                UseBoosting = true,
                BoostFields = new Dictionary<string, byte> { { field, weight } },
                BoostTypes = new Dictionary<Type, sbyte>(),
                SearchText = "foo",
                Language = _language
            });

            MatchWithBoost match =
                request.Query.Bool.Should
                    .OfType<MatchWithBoost>()
                    .FirstOrDefault(s =>
                        s.Match[field].Value<byte>(JsonNames.Boost) == weight
                        && s.Match[field].Value<string>(JsonNames.Query) == field);

            Assert.NotNull(match);
        }

        [Theory]
        [InlineData("Search_Term_Foo.json", "Foo")]
        [InlineData("Search_Term_Foo-Bar.json", "Foo Bar")]
        public void Search_WithQuery_ReturnsExpectedJson(string testFile, string term)
        {
            var expected = RemoveWhitespace(GetJsonTestData(testFile));

            string result = RemoveWhitespace(Serialize(_builder.Search(new QuerySetup
            {
                SearchText = term,
                Language = _language
            })));

            Assert.Contains(expected, result);
        }

        [Theory]
        [InlineData("Search_With_Filter_123_Term_Foo.json", 123, "Foo")]
        [InlineData("Search_With_Filter_678_Term_Bar.json", 678, "Bar")]
        [InlineData("Search_With_Filter_123_Term_Foo_Bar.json", 123, "Foo Bar")]
        public void Search_WithFilter_ReturnsExpectedJson(string testFile, int path, string term)
        {
            var expected = RemoveWhitespace(GetJsonTestData(testFile));

            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var result = RemoveWhitespace(Serialize(builder.Search(new QuerySetup
            {
                RootId = path,
                SearchText = term,
                Language = _language
            })));

            _console.WriteLine(expected);
            _console.WriteLine(result);

            Assert.Contains(expected, result);
        }

        [Fact]
        public void Search_GaussAndScriptScore_Throws()
        {
            var setup = new QuerySetup
            {
                SearchText = GetString(),
                ScriptScore = new ScriptScore
                {
                    Script = new ScriptScore.ScriptScoreInner
                    {
                        Language = "painless",
                        Source = GetString()
                    }
                }
            };

            setup.Gauss.Add(new Gauss());

            Exception exception = Assert.Throws<InvalidOperationException>(() => _builder.Search(setup));

            Assert.Equal("Cannot use Gauss and ScriptScore simultaneously", exception.Message);
        }

        [Fact]
        public void Search_Decay_ReturnsExpectedJson()
        {
            const string expected1 = "function_score";
            const string expected2 = "\"gauss\":{\"foo\":{\"scale\":\"1337s\",\"offset\":\"42s\"}}";

            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var querySetup = new QuerySetup
            {
                SearchText = GetString(),
                Language = _language,
            };

            querySetup.Gauss.Add(new Gauss
            {
                Field = "foo",
                Offset = "42s",
                Scale = "1337s"
            });

            var result = RemoveWhitespace(Serialize(builder.Search(querySetup)));

            Assert.Contains(expected1, result);
            Assert.Contains(expected2, result);
        }

        [Fact]
        public void Search_NoDecay_ReturnsExpectedJson()
        {
            const string expected1 = "function_score";
            const string expected2 = "\"gauss\":{";

            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var querySetup = new QuerySetup
            {
                SearchText = GetString(),
                Language = _language
            };

            var result = RemoveWhitespace(Serialize(builder.Search(querySetup)));

            Assert.DoesNotContain(expected1, result);
            Assert.DoesNotContain(expected2, result);
        }

        [Fact]
        public void Search_ScriptScore_ReturnsExpectedJson()
        {
            const string expected1 = "function_score";
            const string expected2 = "\"script_score\":{\"script\":{\"lang\":\"painless\",\"source\":\"_score*2\"}}";

            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var querySetup = new QuerySetup
            {
                SearchText = GetString(),
                Language = _language,
                ScriptScore = new ScriptScore
                {
                    Script = new ScriptScore.ScriptScoreInner
                    {
                        Language = "painless",
                        Source = "_score*2"
                    }
                }
            };

            var result = RemoveWhitespace(Serialize(builder.Search(querySetup)));

            Assert.Contains(expected1, result);
            Assert.Contains(expected2, result);
        }

        [Fact]
        public void Search_NoScriptScore_ReturnsExpectedJson()
        {
            const string expected1 = "function_score";
            const string expected2 = "\"script_score\":{";

            var builder = new QueryBuilder(null, null, null);
            builder.SetMappedFields(new[] { "bar" });

            var querySetup = new QuerySetup
            {
                SearchText = GetString(),
                Language = _language
            };

            var result = RemoveWhitespace(Serialize(builder.Search(querySetup)));

            Assert.DoesNotContain(expected1, result);
            Assert.DoesNotContain(expected2, result);
        }

        [Fact]
        public void Search_SizeOver10k_Throws()
        {
            var setup = new QuerySetup
            {
                Size = 10001,
                SearchText = GetString(),
                Language = CultureInfo.CurrentCulture
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => _builder.Search(setup));
        }

        [Fact]
        public void Search_FromOver10k_Throws()
        {
            var setup = new QuerySetup
            {
                From = 10001,
                SearchText = GetString(),
                Language = CultureInfo.CurrentCulture
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => _builder.Search(setup));
        }

        [Fact]
        public void Search_IVersionable_WithApplyDefaultFilters_AddsPublishFilters()
        {
            var setup = new QuerySetup
            {
                SearchText = GetString(),
                Type = typeof(IVersionable),
                Language = CultureInfo.CurrentCulture,
                ApplyDefaultFilters = true
            };

            var request = (QueryRequest)_builder.Search(setup);
            var filters = request.Query.Bool.Filter.OfType<Range<DateTime>>();

            Assert.Contains(filters, x => x.RangeSetting.Field == nameof(IVersionable.StopPublish));
            Assert.Contains(filters, x => x.RangeSetting.Field == nameof(IVersionable.StartPublish));
        }

        [Fact]
        public void GetBoosting_TypeWithBoostAttribute_ReturnsAtLeastOneItem()
        {
            var instance = new TypeWithBoosting();
            List<Boost> boosting = _builder.GetBoosting(instance.GetType(), new Dictionary<string, byte>());

            Assert.True(boosting.Count > 0);
        }

        [Fact]
        public void GetBoosting_TypeWithoutBoostAttribute_ReturnsNoItems()
        {
            var instance = new TypeWithoutBoosting();

            List<Boost> boosting = _builder.GetBoosting(instance.GetType(), new Dictionary<string, byte>());

            Assert.Empty(boosting);
        }

        [Fact]
        public void Search_WithOperatorAnd_ReturnsExpectedJson()
        {
            string result = Serialize(_builder.Search(new QuerySetup
            {
                SearchText = "term",
                Operator = Operator.And,
                Language = CultureInfo.CurrentCulture
            }));

            Assert.Contains("\"operator\": \"and\"", result);
        }

        [Fact]
        public void Search_WithOperatorOr_ReturnsExpectedJson()
        {
            string result = Serialize(_builder.Search(new QuerySetup
            {
                SearchText = "term",
                Operator = Operator.Or,
                Language = CultureInfo.CurrentCulture
            }));

            Assert.Contains("\"operator\": \"or\"", result);
        }

        [Fact]
        public void TypedSearch_Object_ReturnsExpectedJson()
        {
            var json = RemoveWhitespace(GetJsonTestData("SearchOfT_Object.json"));

            var result = RemoveWhitespace(Serialize(_builder.TypedSearch<object>(new QuerySetup
            {
                SearchText = "term",
                Operator = Operator.Or,
                Language = _language
            })));

            Assert.Contains(json, result);
        }

        [Fact]
        public void TypedSearch_String_ReturnsExpectedJson()
        {
            var json = RemoveWhitespace(GetJsonTestData("SearchOfT_String.json"));

            var result = RemoveWhitespace(Serialize(_builder.TypedSearch<string>(new QuerySetup
            {
                SearchText = "term",
                Language = _language
            })));

            Assert.Contains(json, result);
        }

        [Theory]
        [InlineData(Int32.MinValue)]
        [InlineData(Int32.MaxValue)]
        [InlineData(0)]
        [InlineData(1234)]
        public void Filter_ReturnsExpectedJsonForInteger(int value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(int), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldInt_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData(Int64.MinValue)]
        [InlineData(Int64.MaxValue)]
        [InlineData(0)]
        [InlineData(1234)]
        public void Filter_ReturnsExpectedJsonForLong(long value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(long), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldLong_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Filter_ReturnsExpectedJsonForBool(bool value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(bool), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldBool_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("LoremIpsumBaconOmgLongStringMkay")]
        public void Filter_ReturnsExpectedJsonForString(string value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(string), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldString_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData(123456f)]
        [InlineData(-123456f)]
        [InlineData(0f)]
        public void Filter_ReturnsExpectedJsonForFloat(float value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(float), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldFloat_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData(123456)]
        [InlineData(-123456)]
        public void Filter_ReturnsExpectedJsonForDouble(double value)
        {
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(double), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData($"PostFilterShouldDouble_{value}.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Filter_ReturnsExpectedJsonForDate()
        {
            var value = new DateTime(1980, 1, 30);
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(DateTime), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData("PostFilterShouldDate.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Filter_ReturnsExpectedJsonForDateTime()
        {
            var value = new DateTime(1980, 1, 30, 23, 59, 59);
            var setup = new QuerySetup { SearchText = "term", Language = _language };
            setup.Filters.Add(new Filter("MyField", value, typeof(DateTime), false, Operator.And));

            var query = (QueryRequest)_builder.TypedSearch<string>(setup);

            var result = Serialize(query.PostFilter.Bool.Must);
            var expected = GetJsonTestData("PostFilterShouldDateTime.json");

            Assert.Equal(expected, result, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void SortScript_SerializesCorrect()
        {
            var setup = new QuerySetup
            {
                SearchText = "term",
                Language = _language,
                SortFields = new List<Sort>
                {
                    new ScriptSort
                    {
                        Direction = "asc",
                        Script = "123",
                        Type = "number",
                        Language = "painless"
                    }
                }
            };

            var request = (QueryRequest)_builder.TypedSearch<string>(setup);

            var sort = RemoveWhitespace(request.Sort[0].ToString());
            Assert.Contains("\"_script\":{", sort);
            Assert.Contains("\"order\":\"asc\"", sort);
            Assert.Contains("\"type\":\"number\"", sort);
            Assert.Contains("\"lang\":\"painless\"", sort);
            Assert.Contains($"\":\"123\"", sort);
        }

        [Fact]
        public void SortScript_WithParams_SerializesCorrect()
        {
            var setup = new QuerySetup
            {
                SearchText = "term",
                Language = _language,
                SortFields = new List<Sort>
                {
                    new ScriptSort
                    {
                        Direction = "asc",
                        Script = "1 + params.Foo",
                        Type = "number",
                        Language = "painless",
                        Parameters = new { Foo = 42 }
                    }
                }
            };

            var request = (QueryRequest)_builder.TypedSearch<string>(setup);

            var sort = RemoveWhitespace(request.Sort[0].ToString());
            Assert.Contains("\"_script\":{", sort);
            Assert.Contains("\"order\":\"asc\"", sort);
            Assert.Contains("\"type\":\"number\"", sort);
            Assert.Contains("\"params\":{\"Foo\":42}", sort);
            Assert.Contains("\"lang\":\"painless\"", sort);
            Assert.Contains($"\":\"1+params.Foo\"", sort);
        }

        [Fact]
        public void FilterACL_AddsBoolShoulds()
        {
            var setup = new QuerySetup
            {
                SearchText = "term",
                Language = _language,
                AclPrincipal = GetPrincipalInfo("foo", "Role1", "Role2"),
                AppendAclFilters = true
            };

            var request = (QueryRequest)_builder.TypedSearch<string>(setup);
            var boolQuery = request.Query.Bool.Filter.Cast<NestedBoolQuery>().First().Bool;
            var shoulds = boolQuery.Should.Cast<MatchSimple>();

            Assert.Contains(shoulds, f => f.Match.Value<string>("_acl") == "U:foo");
            Assert.Contains(shoulds, f => f.Match.Value<string>("_acl") == "R:Role1");
            Assert.Contains(shoulds, f => f.Match.Value<string>("_acl") == "R:Role2");
        }

        [Theory]
        [InlineData("Foo", MappingType.Text, "Foo.keyword")]
        [InlineData("Bar", MappingType.Integer, "Bar")]
        public void FacetFieldNames_AddsAggregation(string field, MappingType type, string expectedKey)
        {
            var setup = new QuerySetup
            {
                SearchText = "term",
                Language = _language,
                FacetFieldNames = new Dictionary<string, MappingType>
                {
                    { field, type}
                }
            };

            var request = (QueryRequest)_builder.TypedSearch<string>(setup);
            var aggregation = request.Aggregation.First();

            Assert.Equal(expectedKey, aggregation.Key);
        }

        [Fact]
        public void GetQuery_AddsMatchAll()
        {
            var request = (QueryRequest)_builder.Search(new QuerySetup
            {
                IsGetQuery = true,
                SearchText = String.Empty
            });

            var result = request.Query.Bool.Must.Cast<MatchAll>().Any();

            Assert.True(result);
        }

        private static string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
