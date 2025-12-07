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
        
        [JsonIgnore]
        public DatabaseType DatabaseType { get; set; }
    }

    public class Settings
    {
        /// <summary>
        /// Maximum number of rows to copy per table
        /// </summary>
        [JsonProperty("maxRowCount")]
        public int MaxRowCount { get; set; } = 100;
        
        /// <summary>
        /// Password for the target database connection. We default to a strong password for anyway
        /// </summary>
        [JsonProperty("adminPassword")]
        public string AdminPassword { get; set; } = "SuperSecretPassword123!";
    }
}
