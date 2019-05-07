using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class IndexConfiguration : ConfigurationElement
    {
        internal const string InvalidCharacters = "~!#$%^&* ()[]{};'\"|\\.:";

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = InvalidCharacters)]
        public string Name
        {
            get => (string)this["name"];
            set => this["name"] = value;
        }

        [ConfigurationProperty("synonymsFile", IsRequired = false)]
        public string SynonymsFile
        {
            get => (string)this["synonymsFile"];
            set => this["synonymsFile"] = value;
        }

        [ConfigurationProperty("default", DefaultValue = false, IsRequired = false)]
        public bool Default
        {
            get => (bool)this["default"];
            set => this["default"] = value;
        }

        [ConfigurationProperty("type", IsRequired = false)]
        public string Type
        {
            get => (string)this["type"];
            set => this["type"] = value;
        }
    }
}
