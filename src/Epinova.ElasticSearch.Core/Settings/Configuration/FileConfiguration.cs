using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class FileConfiguration : ConfigurationElement
    {
        internal const string InvalidCharacters = "~!#$%^&* ()[]{};'\"|\\:";

        [ConfigurationProperty("extension", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = InvalidCharacters)]
        public string Extension
        {
            get => (string)this["extension"];
            set => this["extension"] = value;
        }
    }
}
