using Newtonsoft.Json;

namespace Kopi.Core.Models.Common
{
    public class KopiConfig
    {
        [JsonProperty("sourceConnectionString")]
        public string SourceConnectionString { get; set; }
        [JsonProperty("tables")]
        public List<string> Tables { get; set; } = [];
        [JsonProperty("settings")]
        public Settings Settings { get; set; }
        
        [JsonIgnore]
        public string ConfigFileFullPath { get; set; }
    }

    public class Settings
    {
        /// <summary>
        /// Maximum number of rows to copy per table
        /// </summary>
        [JsonProperty("maxRowCount")]
        public int MaxRowCount { get; set; } = 100;
        
        /// <summary>
        /// Password for the 'sa' SQL Server admin user in the Kopi Docker container
        /// </summary>
        [JsonProperty("saPassword")]
        public string SaPassword { get; set; } = "SuperSecretPassword123!";
    }
}
