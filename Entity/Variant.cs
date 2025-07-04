using Newtonsoft.Json;
using System.Collections.Generic;

namespace Asiup_Clone_Product.Entity
{
    public class Variant
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public float Price { get; set; }

        [JsonProperty("regular_price")]
        public float RegularPrice { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("attributes")]
        public List<Attribute> Attributes { get; set; }

        public Variant()
        {
            Attributes = new List<Attribute>();
        }
    }
}
