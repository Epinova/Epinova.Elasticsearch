using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Analyzers
    {
        internal static dynamic GetAnalyzerSettings(string languageCode, string synonymsFilePath)
        {
            string languageName = Language.GetLanguageAnalyzer(languageCode);

            dynamic synonymSettings = new { type = "synonym", synonyms = new[] { Constants.DefaultSynonym } };
            if(!String.IsNullOrWhiteSpace(synonymsFilePath))
            {
                synonymSettings = new { type = "synonym", synonyms_path = languageCode + "_" + synonymsFilePath };
            }

            var stemmerLanguage = languageName;
            if(languageName == "french" || languageName == "german")
            {
                stemmerLanguage = "light_" + stemmerLanguage;
            }

            return new
            {
                settings = new
                {
                    analysis = new
                    {
                        filter = CreateFilter(languageName, synonymSettings, stemmerLanguage),
                        char_filter = analysisCharFilter,
                        analyzer = CreateAnalyzer(languageName),
                        tokenizer = triGramTokenizer
                    }
                }
            };
        }

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
            var filter = new[] { "lowercase", languageName + "_stop" };

            if(languageName == "fallback")
            {
                filter = new[] { "lowercase" };
            }

            return new
            {
                filter,
                char_filter = analyzerCharFilter,
                tokenizer = "uax_url_email"
            };
        }

        private static dynamic CreateFilter(string languageName, dynamic synonymSettings, string stemmerLanguage)
        {
            var filter = new ExpandoObject();
            var dict = (IDictionary<string, object>)filter;

            dict.Add("shingle_filter", shingleFilter);

            if(languageName == "french")
            {
                dict.Add("french_elision", new { type = "elision", articles_case = true, articles = new[] { "l", "m", "t", "qu", "n", "s", "j", "d", "c", "jusqu", "quoiqu", "lorsqu", "puisqu" } });
            }

            if(languageName == "english")
            {
                dict.Add("light_english_stemmer", new { type = "stemmer", language = "light_english" });
                dict.Add("english_possessive_stemmer", new { type = "stemmer", language = "possessive_english" });
            }

            if(languageName != "fallback")
            {
                dict.Add(languageName + "_stemmer", new { type = "stemmer", language = stemmerLanguage });
                dict.Add(languageName + "_stop", new { type = "stop", stopwords = "_" + languageName + "_" });
            }

            dict.Add(languageName + "_synonym_filter", synonymSettings);

            return filter;
        }

        private static IEnumerable<string> CreateSuggestFilter(string languageName)
        {
            yield return "lowercase";

            if(languageName == "english")
            {
                yield return "english_possessive_stemmer";
                yield return "light_english_stemmer";
            }
            else if (languageName != "fallback")
            {
                yield return languageName + "_stemmer";
                yield return languageName + "_stop";
            }

            yield return "shingle_filter";
        }

        private static dynamic CreateSimpleAnalyzer(string languageName)
        {
            return new
            {
                filter = CreateSimpleAnalyzerFilter(languageName),
                char_filter = analyzerCharFilter,
                tokenizer = "standard"
            };
        }

        private static dynamic CreateAnalyzer(string languageName)
        {
            var analyzer = new ExpandoObject();
            var dict = (IDictionary<string, object>)analyzer;

            dict.Add(languageName, new
            {
                filter = CreateAnalyzerFilter(languageName),
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

            dict.Add(languageName + "_simple", CreateSimpleAnalyzer(languageName));

            dict.Add(languageName + "_suggest", new { filter = CreateSuggestFilter(languageName), type = "custom", tokenizer = "standard" });

            return analyzer;
        }

        private static IEnumerable<string> CreateSimpleAnalyzerFilter(string languageName)
        {
            if(languageName == "french")
            {
                yield return "french_elision";
            }

            yield return "lowercase";

            yield return languageName + "_synonym_filter";

            if (languageName != "fallback")
            {
                yield return languageName + "_stop";
            }

            if(languageName == "german")
            {
                yield return "german_normalization";
            }
        }

        private static IEnumerable<string> CreateAnalyzerFilter(string languageName)
        {
            if(languageName == "french")
            {
                yield return "french_elision";
            }

            yield return "lowercase";

            if(languageName == "english")
            {
                yield return "english_possessive_stemmer";
            }

            yield return languageName + "_synonym_filter";

            if(languageName != "fallback")
            {
                yield return languageName + "_stop";
                yield return languageName + "_stemmer";
            }

            if(languageName == "german")
            {
                yield return "german_normalization";
            }
        }
    }
}