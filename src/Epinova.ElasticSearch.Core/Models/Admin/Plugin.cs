using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Admin
{
    public class Plugin
    {
        public Plugin()
        {
        }

        public Plugin(string component, string version)
        {
            Component = component;
            Version = new Version(version);
        }

        [JsonProperty(JsonNames.Component)]
        public string Component { get; set; }

        [JsonProperty(JsonNames.PluginVersion)]
        public Version Version { get; set; } = new Version();

        public override string ToString()
            => $"{Component}: v{Version}";
    }
}