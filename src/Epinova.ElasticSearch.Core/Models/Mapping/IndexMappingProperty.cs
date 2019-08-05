using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Mapping
{
    internal class IndexMappingProperty
    {
        [JsonProperty(JsonNames.Analyzer, Order = 10)]
        public string Analyzer { get; set; }

        [JsonProperty(JsonNames.Type, Order = 20)]
        public string Type { get; set; }

        [JsonProperty(JsonNames.Fields, Order = 30)]
        public ContentProperty Fields { get; set; }

        [JsonProperty(JsonNames.Format)]
        public string Format { get; set; }

        [JsonProperty(JsonNames.FieldData)]
        public bool? FieldData { get; set; }

        [JsonProperty(JsonNames.CopyTo)]
        public string[] CopyTo { get; set; }

        [JsonProperty(JsonNames.MappingIndex, Order = 200)]
        public string Index { get; set; }

        [JsonProperty(JsonNames.Properties)]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty(JsonNames.Dynamic)]
        public bool? Dynamic { get; set; }

        public override string ToString()
            => $"Analyzer: {Analyzer}, Format: {Format}, FieldData: {FieldData}, Type: {Type}, CopyTo: {String.Join(",", CopyTo ?? Enumerable.Empty<string>())}, Index: {Index}";

        internal class ContentProperty
        {
            [JsonProperty(JsonNames.Content)]
            public ContentDetails Content { get; set; }

            [JsonProperty(JsonNames.Keyword)]
            public Keyword KeywordSettings { get; set; }

            internal class Keyword
            {
                [JsonProperty(JsonNames.Type)]
                public string Type { get; set; }

                [JsonProperty(JsonNames.IgnoreAbove)]
                public int IgnoreAbove { get; set; }
            }

            internal class ContentDetails
            {
                [JsonProperty(JsonNames.Type)]
                public string Type { get; set; }

                [JsonProperty(JsonNames.TermVector)]
                public string TermVector { get; set; }

                [JsonProperty(JsonNames.Store)]
                public bool? Store { get; set; }
            }
        }

        #region Generated equality members

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
            {
                return false;
            }

            if(ReferenceEquals(this, obj))
            {
                return true;
            }

            if(obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((IndexMappingProperty)obj);
        }

        protected bool Equals(IndexMappingProperty other)
        {
            return System.String.Equals(Index, other.Index) && System.String.Equals(Analyzer, other.Analyzer) &&
                   System.String.Equals(Type, other.Type) && System.String.Equals(Format, other.Format) &&
                   FieldData == other.FieldData &&
                   (CopyTo == null && other.CopyTo == null || CopyTo?.SequenceEqual(other.CopyTo) == true);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Index?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Analyzer?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Type?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Format?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ FieldData.GetHashCode();
                hashCode = (hashCode * 397) ^ (CopyTo?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        #endregion
    }
}