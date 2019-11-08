using System.Configuration;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class IndicesCollection : ConfigurationElementCollection
    {
        public IndexConfiguration this[int index]
        {
            get => (IndexConfiguration)BaseGet(index);
            set
            {
                if(BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(IndexConfiguration serviceConfig)
            => BaseAdd(serviceConfig);

        public void Clear()
            => BaseClear();

        protected override ConfigurationElement CreateNewElement()
            => new IndexConfiguration();

        protected override object GetElementKey(ConfigurationElement element)
            => (IndexConfiguration)element;

        public void Remove(IndexConfiguration serviceConfig)
            => BaseRemove(serviceConfig);

        public void Remove(string name)
            => BaseRemove(name);

        public void RemoveAt(int index)
            => BaseRemoveAt(index);
    }
}
