using System;

namespace Epinova.ElasticSearch.Core.Attributes
{
    /// <summary>
    /// Indicates that the property should be scored higher. Weight defaults to 1 if not specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BoostAttribute : Attribute
    {
        public BoostAttribute()
            : this(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoostAttribute"/> class 
        /// and sets the value of <see cref="Weight"/>
        /// </summary>
        /// <param name="weight">The weight to set</param>
        public BoostAttribute(int weight)
        {
            Weight = weight;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Weight { get; set; }
    }
}