using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class ContentSelectorConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true, IsKey = true)]
        public int Id
        {
            get => (int)this["id"];
            set => this["id"] = value;
        }

        [ConfigurationProperty("provider", IsRequired = true)]
        public string Provider
        {
            get => (string)this["provider"];
            set => this["provider"] = value;
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get => (string)this["type"];
            set => this["type"] = value;
        }
    }
}
