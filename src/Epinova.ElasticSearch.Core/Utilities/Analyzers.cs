using System.Collections.Concurrent;
// ReSharper disable InconsistentNaming

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Analyzers
    {
        internal static readonly ConcurrentDictionary<string, dynamic> List;

        internal static readonly dynamic Shingle = new
        {
            settings = new
            {
                analysis = new
                {
                    filter = new
                    {
                        light_english_stemmer = new {
                            type = "stemmer",
                            language = "light_english"
                        },
                        english_possessive_stemmer = new {
                            type = "stemmer",
                            language = "possessive_english"
                        },
                        shingle_filter = new
                        {
                            type = "shingle",
                            min_shingle_size = 2,
                            max_shingle_size = 4
                        }
                    }
                }
            }
        };

        internal static readonly dynamic Raw = new { settings = new { analysis = new {
                analyzer = new {
                    raw = new {
                        filter = new[] { "lowercase", "asciifolding"},
                        type = "custom",
                        tokenizer = "keyword"
                    }
                }}}};

        internal static dynamic GetSuggestAnalyzer(string languageName)
        {
            return new { settings = new { analysis = new {
                analyzer = new {
                    suggest = new {
                        filter = new[] { "lowercase", languageName + "_stop" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "uax_url_email"
                    }
                }}}};
        }

        internal static readonly dynamic TriGramTokenizer = new { settings = new { analysis = new {
                tokenizer = new {
                    trigram_tokenizer = new {
                        token_chars = new [] { "letter","digit" },
                        min_gram = "3",
                        type = "ngram",
                        max_gram = "3"
                    }
                }}}};

        static Analyzers()
        {
            List = new ConcurrentDictionary<string, dynamic>();

            var synonymFilter = new { ignore_case = true, type = "synonym", synonyms = new[] { "example_from,example_to" } };

            dynamic dutch = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    dutch_stop = new { type = "stop", stopwords = "_dutch_" },
                    dutch_stemmer = new { type = "stemmer", language = "dutch" },
                    dutch_synonym_filter = synonymFilter
                },
                analyzer = new  {
                    dutch = new {
                        tokenizer = "standard",
                        filter = new[] { "lowercase", "dutch_stop", "dutch_synonym_filter", "dutch_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    dutch_simple = new
                    {
                        filter = new[] { "lowercase", "dutch_stop", "dutch_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic dutch_suggest = new { settings = new { analysis = new { analyzer = new {
                dutch_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "dutch_stemmer",
                        "dutch_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic swedish = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    swedish_stop = new { type = "stop", stopwords = "_swedish_" },
                    swedish_stemmer = new { type = "stemmer", language = "swedish" },
                    swedish_synonym_filter = synonymFilter
                },
                analyzer = new  {
                    swedish = new {
                        tokenizer = "standard",
                        filter = new[] { "lowercase", "swedish_stop", "swedish_synonym_filter", "swedish_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    swedish_simple = new
                    {
                        filter = new[] { "lowercase", "swedish_stop", "swedish_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic swedish_suggest = new { settings = new { analysis = new { analyzer = new {
                swedish_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "swedish_stemmer",
                        "swedish_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic german = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    german_stop = new { type = "stop", stopwords = "_german_" },
                    german_stemmer = new { type = "stemmer", language = "light_german" },
                    german_synonym_filter = synonymFilter
                },
                analyzer = new {
                    german = new {
                        tokenizer = "standard",
                        filter = new[] { "lowercase", "german_stop", "german_normalization", "german_synonym_filter", "german_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    german_simple = new
                    {
                        filter = new[] { "lowercase", "german_stop", "german_normalization", "german_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic german_suggest = new { settings = new { analysis = new { analyzer = new {
                german_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "german_stemmer",
                        "german_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic french = new { settings = new { analysis = new {
                char_filter = new
                {
                    dot_strip = new
                    {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    french_elision = new { type = "elision", articles_case = true, articles = new[] { "l","m","t","qu","n","s","j","d","c","jusqu","quoiqu","lorsqu","puisqu" } },
                    french_stop = new { type = "stop", stopwords = "_french_" },
                    french_stemmer = new { type = "stemmer", language = "light_french" },
                    french_synonym_filter = synonymFilter
                },
                analyzer = new {
                    french = new {
                        tokenizer = "standard",
                        filter = new[] { "french_elision", "lowercase", "french_stop", "french_synonym_filter", "french_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    french_simple = new
                    {
                        filter = new[] { "french_elision", "lowercase", "french_stop", "french_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic french_suggest = new { settings = new { analysis = new { analyzer = new {
                french_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "french_stemmer",
                        "french_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic norwegian = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    norwegian_stop = new { type = "stop", stopwords = "_norwegian_" },
                    norwegian_synonym_filter = synonymFilter,
                    norwegian_stemmer = new { type = "stemmer", language = "norwegian" }
                },
                analyzer = new {
                    norwegian = new {
                        filter = new[] { "lowercase", "norwegian_stop", "norwegian_synonym_filter", "norwegian_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    },
                    norwegian_simple = new
                    {
                        filter = new[] { "lowercase", "norwegian_stop", "norwegian_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic norwegian_suggest = new { settings = new { analysis = new { analyzer = new {
                norwegian_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "norwegian_stemmer",
                        "norwegian_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic danish = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    danish_stop = new { type = "stop", stopwords = "_danish_" },
                    danish_stemmer = new { type = "stemmer", language = "danish" },
                    danish_synonym_filter = synonymFilter
                },
                analyzer = new {
                    danish = new {
                        tokenizer = "standard",
                        filter = new[] { "lowercase", "danish_stop", "danish_synonym_filter", "danish_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    danish_simple = new
                    {
                        filter = new[] { "lowercase", "danish_stop", "danish_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic danish_suggest = new { settings = new { analysis = new { analyzer = new {
                danish_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "danish_stemmer",
                        "danish_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic spanish = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    spanish_stop = new { type = "stop", stopwords = "_spanish_" },
                    spanish_stemmer = new { type = "stemmer", language = "spanish" },
                    spanish_synonym_filter = synonymFilter
                },
                analyzer = new {
                    spanish = new {
                        tokenizer = "standard",
                        filter = new[] { "lowercase", "spanish_stop", "spanish_synonym_filter", "spanish_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    spanish_simple = new
                    {
                        filter = new[] { "lowercase", "spanish_stop", "spanish_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" },
                        tokenizer = "standard"
                    }
                }}}};

            dynamic spanish_suggest = new { settings = new { analysis = new { analyzer = new {
                spanish_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "spanish_stemmer",
                        "spanish_stop",
                        "shingle_filter"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            dynamic english = new { settings = new { analysis = new {
                char_filter = new {
                    dot_strip = new {
                        type = "pattern_replace",
                        pattern = "\\.",
                        replacement = " "
                    }
                },
                filter = new {
                    english_stop = new { type = "stop", stopwords = "_english_" },
                    english_stemmer = new { type = "stemmer", language = "english" },
                    english_synonym_filter = synonymFilter,
                    english_possessive_stemmer = new { type = "stemmer", language = "possessive_english" }
                },
                analyzer = new {
                    english = new {
                        tokenizer = "standard",
                        filter = new[] { "english_possessive_stemmer", "lowercase", "english_stop", "english_synonym_filter", "english_stemmer" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    },
                    english_simple = new
                    {
                        tokenizer = "standard",
                        filter = new[] { "english_possessive_stemmer", "lowercase", "english_stop", "english_synonym_filter" },
                        char_filter = new[] { "html_strip", "dot_strip" }
                    }
                }}}};

            dynamic english_suggest = new { settings = new { analysis = new { analyzer = new {
                english_suggest = new {
                    filter = new []
                    {
                        "lowercase",
                        "shingle_filter",
                        "english_possessive_stemmer",
                        "english_stop",
                        "light_english_stemmer"
                    },
                    type = "custom",
                    tokenizer = "standard"
                }}}}};

            List.TryAdd("dutch", dutch);
            List.TryAdd("dutch_suggest", dutch_suggest);
            List.TryAdd("swedish", swedish);
            List.TryAdd("swedish_suggest", swedish_suggest);
            List.TryAdd("german", german);
            List.TryAdd("german_suggest", german_suggest);
            List.TryAdd("french", french);
            List.TryAdd("french_suggest", french_suggest);
            List.TryAdd("danish", danish);
            List.TryAdd("danish_suggest", danish_suggest);
            List.TryAdd("spanish", spanish);
            List.TryAdd("spanish_suggest", spanish_suggest);
            List.TryAdd("norwegian", norwegian);
            List.TryAdd("norwegian_suggest", norwegian_suggest);
            List.TryAdd("english", english);
            List.TryAdd("english_suggest", english_suggest);
        }
    }
}