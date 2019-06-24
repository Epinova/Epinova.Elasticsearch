using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        internal static readonly List<string> Extensions = new List<string>();

        internal static string[] IncludedFileExtensions => Extensions.ToArray();

        /// <summary>
        /// Include the specified file-extension in the index
        /// </summary>
        /// <param name="extension">The extension, e.g. pdf, doc, xls</param>
        /// <returns>The <see cref="Indexing"/> instance</returns>
        public Indexing IncludeFileType(string extension)
        {
            var config = ElasticSearchSection.GetConfiguration();
            if(!config.Files.Enabled)
            {
                Logger.Information($"Not adding '{extension}', file indexing is disabled");
                return this;
            }

            if(!String.IsNullOrWhiteSpace(extension))
            {
                Extensions.Add(extension.Trim(' ', '.').ToLower());
            }

            return this;
        }
    }
}