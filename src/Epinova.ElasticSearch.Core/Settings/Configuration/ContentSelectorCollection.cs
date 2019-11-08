using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class ContentSelectorCollection : ConfigurationElementCollection
    {
        public ContentSelectorConfiguration this[int index]
        {
            get => (ContentSelectorConfiguration)BaseGet(index);
            set
            {
                if(BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ContentSelectorConfiguration serviceConfig)
            => BaseAdd(serviceConfig);

        public void Clear()
            => BaseClear();

        protected override ConfigurationElement CreateNewElement()
            => new ContentSelectorConfiguration();

        protected override object GetElementKey(ConfigurationElement element)
            => (ContentSelectorConfiguration)element;

        public void Remove(ContentSelectorConfiguration serviceConfig)
            => BaseRemove(serviceConfig);

        public void Remove(string name)
            => BaseRemove(name);

        public void RemoveAt(int index)
            => BaseRemoveAt(index);
    }
}
