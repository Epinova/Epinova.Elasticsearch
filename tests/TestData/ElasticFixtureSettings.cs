using System;
using Epinova.ElasticSearch.Core.Models;

namespace TestData
{
    public static class ElasticFixtureSettings
    {
        public const string PluginName = "mapper-attachments";
        public static string Language;
        public const string LanguageName = "norwegian";
        public static string IndexName;
        public static readonly Type IndexType = typeof(IndexItem);
    }
}