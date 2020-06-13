using System;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// Contains methods for configuring custom properties on indexed items
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class CustomPropertyConvention<T>
    {
        private readonly Indexing _instance;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(CustomPropertyConvention<T>));

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomPropertyConvention{T}"/> class
        /// </summary>
        /// <param name="instance">The <see cref="Indexing"/> instance</param>
        public CustomPropertyConvention(Indexing instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// Include a custom property when indexing this type.
        /// The name of the property will be the same as the property/method in the <paramref name="fieldSelector"/> parameter.
        /// <para>
        /// If you need more control over the name, and/or the method supplying the data to be indexed has
        /// no relations to the type, use the overload <c>IncludeField(string, Expression, bool)</c>
        /// </para>
        /// </summary>
        /// <example>
        /// <para>Extension/instance method: IncludeField(x => x.CustomStuff());</para>
        /// <para>Property method: IncludeField(x => x.MyCustomProp);</para>
        /// <para>Custom name: IncludeField("Foobar", x => x.MyCustomProp);</para>
        /// </example>
        /// <param name="fieldSelector">An expression, typically a property or an instance/extension method.</param>
        /// <param name="stem">Should stemming be applied for this property?</param>
        public Indexing IncludeField<TProperty>(Expression<Func<T, TProperty>> fieldSelector, bool stem = false)
        {
            string fieldName = ElasticSearchService<T>.GetFieldInfo(fieldSelector).Item1;

            return IncludeField(fieldName, fieldSelector, stem);
        }

        /// <summary>
        /// Include a custom property when indexing this type.
        /// </summary>
        /// <param name="fieldName">The name the property will be indexed as</param>
        /// <param name="fieldSelector">An expression, typically a property or an instance/extension method.</param>
        /// <param name="stem">Should stemming be applied for this property?</param>
        public Indexing IncludeField<TProperty>(string fieldName, Expression<Func<T, TProperty>> fieldSelector, bool stem = false)
        {
            _logger.Debug("Including field: " + fieldName);

            if(!String.IsNullOrEmpty(fieldName))
            {
                //Is compile needed?
                Func<T, TProperty> getter = fieldSelector.Compile();

                Indexing.CustomProperties.Add(new CustomProperty(fieldName, getter, typeof(T)));
            }

            if(stem)
            {
                StemField(fieldSelector);
            }

            return _instance;
        }

        /// <summary>
        /// Apply stemming for field <paramref name="fieldSelector"/>
        /// </summary>
        /// <param name="fieldSelector">An expression, typically a property or an instance/extension method.</param>
        public Indexing StemField<TProperty>(Expression<Func<T, TProperty>> fieldSelector)
        {
            var fieldName = ElasticSearchService<T>.GetFieldInfo(fieldSelector).Item1;

            if(WellKnownProperties.Analyze.Contains(fieldName))
            {
                return _instance;
            }

            _logger.Debug("Adding stemming for field: " + fieldName);
            WellKnownProperties.Analyze.Add(fieldName);

            return _instance;
        }

        /// <summary>
        /// Enables highlighting on the supplied field(s).
        /// Fields named MainIntro, MainBody and Description are included by default
        /// </summary>
        public Indexing EnableHighlighting<TProperty>(params Expression<Func<T, TProperty>>[] fieldSelectors)
        {
            if(fieldSelectors == null || fieldSelectors.Length == 0)
            {
                return _instance;
            }

            foreach(var fieldSelector in fieldSelectors)
            {
                string fieldName = ElasticSearchService<T>.GetFieldInfo(fieldSelector).Item1;

                if(!Indexing.Highlights.Contains(fieldName))
                {
                    Indexing.Highlights.Add(fieldName);
                }
            }

            return _instance;
        }

        /// <summary>
        /// Adds suggestions for all properties of {T}, except those hidden by convention of configuration
        /// </summary>
        public Indexing EnableSuggestions()
        {
            // Update existing registration of type?
            if(Indexing.Suggestions.Any(s => s.Type == typeof(T)))
            {
                Indexing.Suggestions.Single(s => s.Type == typeof(T)).IncludeAllFields = true;
            }
            else
            {
                Indexing.Suggestions.Add(new Suggestion(typeof(T), true));
            }

            return _instance;
        }

        /// <summary>
        /// Adds suggestions for supplied properties of {T}, except those hidden by convention of configuration
        /// </summary>
        public Indexing EnableSuggestions<TProperty>(params Expression<Func<T, TProperty>>[] fieldSelectors)
        {
            if(fieldSelectors == null || fieldSelectors.Length == 0)
            {
                return _instance;
            }

            foreach(var fieldSelector in fieldSelectors)
            {
                string fieldName = ElasticSearchService<T>.GetFieldInfo(fieldSelector).Item1;

                // Update existing registration of type?
                if(Indexing.Suggestions.Any(s => s.Type == typeof(T)))
                {
                    Indexing.Suggestions.Single(s => s.Type == typeof(T)).InputFields.Add(fieldName);
                }
                else
                {
                    var suggestion = new Suggestion(typeof(T));
                    suggestion.InputFields.Add(fieldName);
                    Indexing.Suggestions.Add(suggestion);
                }
            }

            return _instance;
        }
    }
}