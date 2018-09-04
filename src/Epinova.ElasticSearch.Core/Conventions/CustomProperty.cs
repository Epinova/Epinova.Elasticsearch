using System;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// Defines a custom property added at index-time
    /// </summary>
    public class CustomProperty
    {
        /// <summary>
        /// The property name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The property type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The owner type
        /// </summary>
        public Type OwnerType { get; }

        /// <summary>
        /// The property getter method
        /// </summary>
        public Delegate Getter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomProperty"/> class 
        /// </summary>
        public CustomProperty(string name, Delegate getter, Type ownerType)
        {
            Name = name;
            Type = getter.Method.ReturnType;
            OwnerType = ownerType;
            Getter = getter;
        }
    }
}