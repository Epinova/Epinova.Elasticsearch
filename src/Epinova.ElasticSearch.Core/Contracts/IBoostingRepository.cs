using System;
using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IBoostingRepository
    {
        Dictionary<string, int> GetByType(Type type);
        void Save(string typeName, Dictionary<string, int> boosting);
        void DeleteAll();
    }
}