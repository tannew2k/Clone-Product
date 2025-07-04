using System.Collections.Generic;
using Newtonsoft.Json;

namespace Asiup_Clone_Product.Entity
{
    public class Image
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }
        
        [JsonProperty("variant_ids")]
        public List<long> VariantIds { get; set; }

        public Image()
        {
            VariantIds = new List<long>();
        }
    }
}
