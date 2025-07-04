using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Policy;

namespace Asiup_Clone_Product.Entity
{
    public class Attribute
    {
        [JsonIgnore]
        public long Id { get; set; }

        [JsonIgnore]
        public string Slug { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("values")]
        public List<string> Values;

        public Attribute()
        {
            Values = new List<string>();
        }
    }
}
