using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Analyzers
    {
        private static readonly dynamic shingleFilter = new
        {
            type = "shingle",
            min_shingle_size = 2,
            max_shingle_size = 4
        };

        private static readonly dynamic analysisCharFilter = new
        {
            dot_strip = new
            {
                type = "pattern_replace",
                pattern = "\\.",
                replacement = " "
            }
        };

        private static readonly IEnumerable<string> analyzerCharFilter = new[]
        {
            "html_strip",
            "dot_strip"
        };

        private static readonly dynamic triGramTokenizer = new
        {
            trigram_tokenizer = new
            {
                token_chars = new[] { "letter", "digit" },
                min_gram = "3",
                type = "ngram",
                max_gram = "3"
            }
        };

        private static dynamic GetSuggestAnalyzer(string languageName)
        {
            return new
            {
                filter = new[] { "lowercase", languageName + "_stop" },
                char_filter = analyzerCharFilter,
                tokenizer = "uax_url_email"
            };
        }

        internal static dynamic GetAnalyzerSettings(string languageCode, string synonymsFilePath)
        {
            string languageName = Language.GetLanguageAnalyzer(languageCode);

            dynamic synonymSettings = new { type = "synonym", synonyms = new[] { "example_from,example_to" } };
            if (!String.IsNullOrWhiteSpace(synonymsFilePath))
            {
                synonymSettings = new { type = "synonym", synonyms_path = languageCode + "_" + synonymsFilePath };
            }

            var stemmerLanguage = languageName;
            if (languageName == "french" || languageName == "german")
                stemmerLanguage = "light_" + stemmerLanguage;

            IEnumerable<string> CreateAnalyzerFilter()
            {
                if (languageName == "french")
                    yield return "french_elision";

                yield return "lowercase";

                if (languageName == "english")
                    yield return "english_possessive_stemmer";

                yield return languageName + "_stop";

                if (languageName == "german")
                    yield return "german_normalization";

                yield return languageName + "_synonym_filter";
                yield return languageName + "_stemmer";
            }

            IEnumerable<string> CreateSimpleAnalyzerFilter()
            {
                if (languageName == "french")
                    yield return "french_elision";

                yield return "lowercase";
                yield return languageName + "_stop";

                if (languageName == "german")
                    yield return "german_normalization";

                yield return languageName + "_synonym_filter";
            }

            dynamic CreateAnalyzer()
            {
                var analyzer = new ExpandoObject();
                var dict = (IDictionary<string, object>)analyzer;

                dict.Add(languageName, new
                {
                    filter = CreateAnalyzerFilter(),
                    char_filter = analyzerCharFilter,
                    tokenizer = "standard"
                });

                dict.Add("raw", new
                {
                    filter = new[] { "lowercase", "asciifolding" },
                    type = "custom",
                    tokenizer = "keyword"
                });

                dict.Add("suggest", GetSuggestAnalyzer(languageName));

                dict.Add(languageName + "_simple", CreateSimpleAnalyzer());

                dict.Add(languageName + "_suggest", new { filter = CreateSuggestFilter(), type = "custom", tokenizer = "standard" });

                return analyzer;
            }

            dynamic CreateSimpleAnalyzer()
            {
                return new
                {
                    filter = CreateSimpleAnalyzerFilter(),
                    char_filter = analyzerCharFilter,
                    tokenizer = "standard"
                };
            }

            IEnumerable<string> CreateSuggestFilter()
            {
                yield return "lowercase";

                if (languageName == "english")
                {
                    yield return "english_possessive_stemmer";
                    yield return "light_english_stemmer";
                }
                else
                {
                    yield return languageName + "_stemmer";
                }

                yield return languageName + "_stop";
                yield return "shingle_filter";
            }

            dynamic CreateFilter()
            {
                var filter = new ExpandoObject();
                var dict = (IDictionary<string, object>)filter;

                dict.Add("shingle_filter", shingleFilter);

                if (languageName == "french")
                {
                    dict.Add("french_elision", new { type = "elision", articles_case = true, articles = new[] { "l", "m", "t", "qu", "n", "s", "j", "d", "c", "jusqu", "quoiqu", "lorsqu", "puisqu" }});
                }

                if (languageName == "english")
                {
                    dict.Add("light_english_stemmer", new { type = "stemmer", language = "light_english" });
                    dict.Add("english_possessive_stemmer", new { type = "stemmer", language = "possessive_english" });
                }

                dict.Add(languageName + "_stop", new { type = "stop", stopwords = "_" + languageName + "_" });
                dict.Add(languageName + "_stemmer", new { type = "stemmer", language = stemmerLanguage });
                dict.Add(languageName + "_synonym_filter", synonymSettings);

                return filter;
            }

            return new
            {
                settings = new
                {
                    analysis = new
                    {
                        filter = CreateFilter(),
                        char_filter = analysisCharFilter,
                        analyzer = CreateAnalyzer(),
                        tokenizer = triGramTokenizer
                    }
                }
            };
        }
    }
}