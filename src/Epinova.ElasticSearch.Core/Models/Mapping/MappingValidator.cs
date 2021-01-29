using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Models.Mapping
{
    public class MappingValidatorType
    {
        public string Name { get; }
        public List<MappingValidatorProperty> Properties { get; }

        public MappingValidatorType(string name, List<MappingValidatorProperty> properties)
        {
            Name = name;
            Properties = properties;
        }
    }

    public class MappingValidatorProperty
    {
        public MappingValidatorProperty(string propertyName, IEnumerable<string> errors)
        {
            PropertyName = propertyName;
            Errors = errors;
        }

        public string PropertyName { get; }
        public IEnumerable<string> Errors { get; }
    }
}
