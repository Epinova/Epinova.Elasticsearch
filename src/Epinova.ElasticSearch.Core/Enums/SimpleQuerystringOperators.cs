using System;
using System.Runtime.Serialization;

namespace Epinova.ElasticSearch.Core.Enums
{
    /// <summary>
    /// Represents operators supported by the simple query string syntax.
    /// </summary>
    [Flags]
    public enum SimpleQuerystringOperators
    {
        /// <summary>
        /// Disables all operators. 
        /// </summary>
        [EnumMember(Value = "NONE")]
        None = 0,

        /// <summary>
        /// Enables the + AND operator. 
        /// </summary>
        [EnumMember(Value = "AND")]
        And = 1,

        /// <summary>
        /// Enables \ as an escape character. 
        /// </summary>
        [EnumMember(Value = "ESCAPE")]
        Escape = 2,

        /// <summary>
        /// Enables the ~N operator after a word, where N is an integer denoting the allowed edit distance for matching. See Fuzziness. 
        /// </summary>
        [EnumMember(Value = "FUZZY")]
        Fuzzy = 4,

        /// <summary>
        /// Enables the ~N operator, after a phrase where N is the maximum number of positions allowed between matching tokens. Synonymous to SLOP. 
        /// </summary>
        [EnumMember(Value = "NEAR")]
        Near = 8,

        /// <summary>
        /// Enables the - NOT operator. 
        /// </summary>
        [EnumMember(Value = "NOT")]
        Not = 16,

        /// <summary>
        /// Enables the \| OR operator. 
        /// </summary>
        [EnumMember(Value = "OR")]
        Or = 32,

        /// <summary>
        /// Enables the " quotes operator used to search for phrases. 
        /// </summary>
        [EnumMember(Value = "PHRASE")]
        Phrase = 64,

        /// <summary>
        /// Enables the ( and ) operators to control operator precedence. 
        /// </summary>
        [EnumMember(Value = "PRECEDENCE")]
        Precedence = 128,

        /// <summary>
        /// Enables the * prefix operator. 
        /// </summary>
        [EnumMember(Value = "PREFIX")]
        Prefix = 256,

        /// <summary>
        /// Enables the ~N operator, after a phrase where N is maximum number of positions allowed between matching tokens. Synonymous to NEAR. 
        /// </summary>
        [EnumMember(Value = "SLOP")]
        Slop = 512,

        /// <summary>
        /// Enables whitespace as split characters.
        /// </summary>
        [EnumMember(Value = "WHITESPACE")]
        Whitespace = 1024,

        /// <summary>
        /// Enables all optional operators. (Default)
        /// </summary>
        [EnumMember(Value = "ALL")]
        All = And | Escape | Fuzzy | Near | Not | Or | Phrase | Precedence | Prefix | Slop | Whitespace
    }
}