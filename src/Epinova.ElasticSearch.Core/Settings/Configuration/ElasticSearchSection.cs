using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;
using System.Web.Hosting;

namespace Epinova.ElasticSearch.Core.Settings.Configuration
{
    public class ElasticSearchSection : ConfigurationSection
    {
        private static readonly string[] ValidSizeSuffixes = { "kb", "mb", "gb" };

        private static readonly char[] ValidSizeChars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'k', 'm', 'g', 'b'
        };

        public static ElasticSearchSection GetConfiguration()
        {
            if(!HostingEnvironment.IsHosted)
            {
                return new ElasticSearchSection();
            }

            var section = WebConfigurationManager
                    .OpenWebConfiguration("~")
                    .GetSection("epinova.elasticSearch")
                as ElasticSearchSection;

            if(section == null)
            {
                throw new ConfigurationErrorsException("epinova.elasticSearch not found");
            }

            section.ValidateIndices();
            section.ValidateFiles();

            return section;
        }

        [ConfigurationProperty("host", IsRequired = true)]
        [StringValidator(InvalidCharacters = "~!#$%^&* ()[]{};'\"|\\")]
        public string Host
        {
            get => (string)this["host"];
            set => this["host"] = value;
        }

        [ConfigurationProperty("username", IsRequired = false)]
        public string Username
        {
            get => (string)this["username"];
            set => this["username"] = value;
        }

        [ConfigurationProperty("password", IsRequired = false)]
        public string Password
        {
            get => (string)this["password"];
            set => this["password"] = value;
        }

        [ConfigurationProperty("trackingConnectionStringName", IsRequired = false)]
        public string TrackingConnectionStringName
        {
            get => (string)this["trackingConnectionStringName"];
            set => this["trackingConnectionStringName"] = value;
        }

        [ConfigurationProperty("bulksize", DefaultValue = 1000, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 1000)]
        public int Bulksize
        {
            get => (int)this["bulksize"];
            set => this["bulksize"] = value;
        }

        [ConfigurationProperty("closeIndexDelay", DefaultValue = 500, IsRequired = false)]
        [IntegerValidator(MinValue = 0, MaxValue = 10000)]
        public int CloseIndexDelay
        {
            get => (int)this["closeIndexDelay"];
            set => this["closeIndexDelay"] = value;
        }

        [ConfigurationProperty("providerMaxResults", DefaultValue = 100, IsRequired = false)]
        [IntegerValidator(MinValue = 10, MaxValue = 1000)]
        public int ProviderMaxResults
        {
            get => (int)this["providerMaxResults"];
            set => this["providerMaxResults"] = value;
        }

        [ConfigurationProperty("ignoreXhtmlStringContentFragments", DefaultValue = false, IsRequired = false)]
        public bool IgnoreXhtmlStringContentFragments
        {
            get => (bool)this["ignoreXhtmlStringContentFragments"];
            set => this["ignoreXhtmlStringContentFragments"] = value;
        }

        [ConfigurationProperty("clientTimeoutSeconds", DefaultValue = 100, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 1800)]
        public int ClientTimeoutSeconds
        {
            get => (int)this["clientTimeoutSeconds"];
            set => this["clientTimeoutSeconds"] = value;
        }

        [ConfigurationProperty("shards", DefaultValue = 5, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 1000)]
        public int NumberOfShards
        {
            get => (int)this["shards"];
            set => this["shards"] = value;
        }

        [ConfigurationProperty("replicas", DefaultValue = 1, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 1000)]
        public int NumberOfReplicas
        {
            get => (int)this["replicas"];
            set => this["replicas"] = value;
        }

        [ConfigurationProperty("indices", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(IndicesCollection))]
        public virtual IndicesCollection Indices
        {
            get => (IndicesCollection)base["indices"];
            set => base["indices"] = value;
        }

        public IEnumerable<IndexConfiguration> IndicesParsed => Indices.OfType<IndexConfiguration>();

        [ConfigurationProperty("files", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(FilesCollection))]
        public virtual FilesCollection Files
        {
            get => (FilesCollection)base["files"];
            set => base["files"] = value;
        }
        
        [ConfigurationProperty("useTls12", DefaultValue = false, IsRequired = false)]
        public bool UseTls12
        {
            get => (bool)this["useTls12"];
            set => this["useTls12"] = value;
        }

        internal bool IsValidSizeString(string size)
        {
            if(String.IsNullOrWhiteSpace(size))
            {
                return false;
            }

            if(Int64.TryParse(size, out long parsed))
            {
                return parsed > 0;
            }

            IEnumerable<char> invalidChars = size.ToLower().ToCharArray().Except(ValidSizeChars);
            if(invalidChars.Any())
            {
                return false;
            }

            if(ValidSizeSuffixes.All(suffix => !size.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        internal void ValidateFiles()
        {
            string[] extensions = Files.OfType<FileConfiguration>().Select(i => i.Extension).ToArray();
            if(extensions.Any(String.IsNullOrWhiteSpace))
            {
                throw new ConfigurationErrorsException("Configuration Error. Extension cannot be empty");
            }

            if(!IsValidSizeString(Files.Maxsize))
            {
                throw new ConfigurationErrorsException("Configuration Error. Maxsize value is invalid");
            }
        }

        internal void ValidateIndices()
        {
            if(!IndicesParsed.Any())
            {
                throw new ConfigurationErrorsException("Configuration Error. You must add at least one index to the <indices> node");
            }

            if(IndicesParsed.Count() > 1 && !IndicesParsed.Any(i => i.Default))
            {
                throw new ConfigurationErrorsException("Configuration Error. One index must be set as default when adding multiple indices");
            }

            if(Indices.Count > 1 && IndicesParsed.Count(i => i.Default) > 1)
            {
                throw new ConfigurationErrorsException("Configuration Error. Only one index can be set as default");
            }

            if(Indices.Count > 1 && IndicesParsed.Count(i => String.IsNullOrWhiteSpace(i.Type)) > 1)
            {
                throw new ConfigurationErrorsException("Configuration Error. Custom indices must define a type");
            }

            // Enumerate indices to trigger StringValidator
            var indices = IndicesParsed.ToArray();

            var indexNames = indices.Select(i => i.Name);
            if(indexNames.Any(String.IsNullOrWhiteSpace))
            {
                throw new ConfigurationErrorsException("Configuration Error. Index name cannot be empty");
            }

            var displayNames = indices.Select(i => i.DisplayName);
            if(displayNames.Any(String.IsNullOrWhiteSpace))
            {
                throw new ConfigurationErrorsException("Configuration Error. Index display name cannot be empty");
            }
        }
    }
}
