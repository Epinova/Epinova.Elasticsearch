using System;
using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class FilesCollection : ConfigurationElementCollection
    {
        [ConfigurationProperty("maxsize", IsRequired = false, DefaultValue = "10240000")]
        public string Maxsize => (string)base["maxsize"];

        internal long ParsedMaxsize
        {
            get
            {
                if(Int64.TryParse(Maxsize, out long bytes))
                {
                    return bytes;
                }

                string lowered = Maxsize.ToLower();

                if(lowered.EndsWith("kb")
                    && Int64.TryParse(lowered.TrimEnd('k', 'b'), out long kbytes))
                {
                    return kbytes * 1024;
                }

                if(lowered.EndsWith("mb")
                    && Int64.TryParse(lowered.TrimEnd('m', 'b'), out long mbytes))
                {
                    return mbytes * (long)Math.Pow(1024, 2);
                }

                if(lowered.EndsWith("gb")
                    && Int64.TryParse(lowered.TrimEnd('g', 'b'), out long gbytes))
                {
                    return gbytes * (long)Math.Pow(1024, 3);
                }

                return 10240000;
            }
        }

        [ConfigurationProperty("enabled", IsRequired = true, DefaultValue = true)]
        public bool Enabled => (bool)base["enabled"];

        [ConfigurationProperty("disableContentIndexing", IsRequired = false, DefaultValue = false)]
        public bool DisableContentIndexing => (bool)base["disableContentIndexing"];

        public FileConfiguration this[int index]
        {
            get => (FileConfiguration)BaseGet(index);
            set
            {
                if(BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(FileConfiguration serviceConfig)
            => BaseAdd(serviceConfig);

        public void Clear()
            => BaseClear();

        protected override ConfigurationElement CreateNewElement()
            => new FileConfiguration();

        protected override object GetElementKey(ConfigurationElement element)
            => (FileConfiguration)element;

        public void Remove(FileConfiguration serviceConfig)
            => BaseRemove(serviceConfig);

        public void Remove(string name)
            => BaseRemove(name);

        public void RemoveAt(int index)
            => BaseRemoveAt(index);
    }
}
