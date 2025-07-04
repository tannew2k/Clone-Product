using Newtonsoft.Json;
using System.Collections.Generic;

namespace Asiup_Clone_Product.Entity
{
    public class Product
    {
        [JsonProperty("variations")]
        public List<Variant> Variants;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("shortDescription")]
        public string ShortDescription;

        [JsonProperty("images")]
        public List<Image> Images;

        [JsonProperty("reviews")]
        public List<Review> Reviews;

        [JsonProperty("attributes")]
        public List<Attribute> Attributes;

        public Product()
        {
            Variants = new List<Variant>();
            Reviews = new List<Review>();
            Attributes = new List<Attribute>();
            Images = new List<Image>();
        }
    }
    
    
}
