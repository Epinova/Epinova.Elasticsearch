using System.Runtime.Serialization;

namespace Epinova.ElasticSearch.Core.Enums
{
    public enum Operator
    {
        [EnumMember(Value = "or")]
        Or,

        [EnumMember(Value = "and")]
        And
    }
}