using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class IndexConfiguration : ConfigurationElement
    {
        internal const string NameInvalidCharacters = "~!#$%^&* ()[]{};'\"|\\.:";

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = NameInvalidCharacters)]
        public string Name
        {
            get => (string)this["name"];
            set => this["name"] = value;
        }

        [ConfigurationProperty("displayName", IsRequired = true)]
        public string DisplayName
        {
            get => (string)this["displayName"];
            set => this["displayName"] = value;
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
