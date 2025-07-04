using Asiup_Clone_Product.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Asiup_Clone_Product.Lib
{
    public enum Platform
    {
        SHOPBASE, SHOPIFY, WOOCOMMERCE, ONE_PAGE, EMPTY, SELLESS
    }

    public class Crawler
    {
        public Platform Platform = Platform.EMPTY;
        public string Id;
        public readonly string Url;
        private readonly ChromeDriver driver;
        public Product Product;

        public Crawler(string url)
        {
            Url = url;
            Id = Guid.NewGuid().ToString();

            var options = new ChromeOptions { AcceptInsecureCertificates = true };
            options.BinaryLocation = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
            //options.AddArgument("--window-size=350,700");
            //options.AddArguments("--disable-infobars");
            //options.AddArguments("--headless");
            //options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

            var chromeDriverService = ChromeDriverService.CreateDefaultService("C:\\Users\\longt\\Downloads\\chromedriver-win64 (1)\\chromedriver-win64\\chromedriver.exe");
            chromeDriverService.HideCommandPromptWindow = true;
            

            driver = new ChromeDriver(chromeDriverService, options);
            driver.Navigate().GoToUrl(Url);

            Product = new Product();
        }

        public void DetectPlatform()
        {
            if (!WaitUntilElementExists(By.TagName("body")))
                return;

            var body = driver.FindElement(By.TagName("body"));
            var bodyClass = body.GetAttribute("class");

            if (!string.IsNullOrEmpty(bodyClass))
            {
                if (bodyClass.Contains("woocommerce-page"))
                {
                    Platform = Platform.WOOCOMMERCE;
                    return;
                }
            }

            if (Platform == Platform.EMPTY)
            {
                var source = driver.FindElement(By.TagName("head")).GetAttribute("innerHTML");
                if (source.Contains("cdn.selless.us"))
                {
                    Platform = Platform.SELLESS;
                    return;
                }

                if (source.Contains("static.wtecdn.net"))
                {
                    Platform = Platform.ONE_PAGE;
                    return;
                }
                
                if (source.Contains("cdn.shopify.com"))
                {
                    Platform = Platform.SHOPIFY;
                    return;
                }

                if (source.Contains("cdn.thesitebase.net"))
                {
                    Platform = Platform.SHOPBASE;
                    return;
                }
            }
        }

        public void CrawlShopify()
        {
            var uri = new Uri(driver.Url);
            driver.Navigate().GoToUrl($"https://{uri.DnsSafeHost}{uri.AbsolutePath}.json");
            WaitUntilElementExists(By.TagName("body"));

            var res = driver.FindElement(By.TagName("pre")).Text;
            var json = JsonConvert.DeserializeObject<dynamic>(res).product;

            Product.Title = json.title.ToString();
            Product.Description = json.body_html.ToString();

            foreach (var image in json.images)
            {
                Product.Images.Add(new Image
                {
                    Id = image.id.ToString(),
                    Src = image.src.ToString()
                });
            }

            foreach (var opt in json.options)
            {
                Product.Attributes.Add(new Entity.Attribute
                {
                    Id = (int)opt.position,
                    Name = opt.name.ToString(),
                    Values = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(opt.values))
                });
            }

            foreach (var item in json.variants)
            {
                var variant = new Variant
                {
                    Image = Product.Images.FirstOrDefault(c => c.Id == item.image_id.ToString())?.Src,
                    Price = (float)item.price,
                    RegularPrice = (float)item.compare_at_price,
                    Name = item.title.ToString()
                };

                for (int i = 1; i <= Product.Attributes.Count; i++)
                {
                    if (item[$"option{i}"] != null)
                    {
                        if (!string.IsNullOrEmpty(item[$"option{i}"].ToString()))
                        {
                            variant.Attributes.Add(new Entity.Attribute
                            {
                                Name = Product.Attributes[i - 1].Name,
                                Value = item[$"option{i}"].ToString()
                            });
                        }
                    }
                }

                Product.Variants.Add(variant);
            }
        }

        public void CrawlShopbase()
        {
            try
            {
                var idProduct = driver.FindElement(By.XPath("//div[section//span/a]/section[1][@selector-id and @active-selector-id]")).GetAttribute("selector-id").ToString().Replace("page-data-", "");
                var uri = new Uri(driver.Url);
                driver.Navigate().GoToUrl($"https://{uri.Host}/api/recsys/cross-sell.json?product_id={idProduct}&source=bundle&rules=best_seller_same_collection,same_collection,same_type,title_similarity,same_tag,lower_price,same_product");
                WaitUntilElementExists(By.TagName("body"));

                var res = driver.FindElement(By.TagName("pre")).Text;
                var json = JsonConvert.DeserializeObject<dynamic>(res).products[0];

                // Initialize collections to avoid null reference issues
                Product.Images = new List<Image>();
                Product.Attributes = new List<Entity.Attribute>();
                Product.Variants = new List<Variant>();

                // Set product title and description
                Product.Title = json.title?.ToString() ?? string.Empty;
                Product.Description = json.description?.ToString() ?? string.Empty;

                // Process images
                foreach (var image in json.images ?? Enumerable.Empty<dynamic>())
                {
                    if (image?.src != null)
                    {
                        Product.Images.Add(new Image
                        {
                            Src = image.src.ToString(),
                            // Store variant_ids for later use in variant image mapping
                            VariantIds = image.variant_ids != null ? JsonConvert.DeserializeObject<List<long>>(JsonConvert.SerializeObject(image.variant_ids)) : new List<long>()
                        });
                    }
                }

                // Process options
                foreach (var opt in json.options ?? Enumerable.Empty<dynamic>())
                {
                    if (opt?.id != null && opt?.name != null && opt?.values != null)
                    {
                        // Extract the "name" field from each value object in opt.values
                        var values = new List<string>();
                        foreach (var value in opt.values ?? Enumerable.Empty<dynamic>())
                        {
                            if (value?.name != null)
                            {
                                values.Add(value.name.ToString());
                            }
                        }

                        Product.Attributes.Add(new Entity.Attribute
                        {
                            Id = (int)opt.id,
                            Name = opt.name.ToString(),
                            Values = values
                        });
                    }
                }

                // Process variants
                foreach (var item in json.variants ?? Enumerable.Empty<dynamic>())
                {
                    if (item == null) continue;

                    // Map variant to its specific image using variant_ids
                    string imageSrc = null;
                    if (item.id != null)
                    {
                        long variantId = (long)item.id;
                        var matchingImage = Product.Images.FirstOrDefault(img => img.VariantIds.Contains(variantId));
                        imageSrc = matchingImage?.Src ?? Product.Images?.FirstOrDefault()?.Src;
                    }

                    var variant = new Variant
                    {
                        Image = imageSrc,
                        Price = item.price != null ? Convert.ToSingle(item.price) : 0f,
                        RegularPrice = item.compare_at_price != null ? Convert.ToSingle(item.compare_at_price) : 0f,
                        Name = item.title?.ToString() ?? string.Empty
                    };

                    // Map option1 ID to its corresponding name from opt.values
                    for (int i = 1; i <= Product.Attributes.Count; i++)
                    {
                        var optionKey = $"option{i}";
                        if (item[optionKey] != null)
                        {
                            string optionValue = item[optionKey].ToString();
                            if (!string.IsNullOrEmpty(optionValue) && optionValue != "0")
                            {
                                // Find the corresponding name for the option1 ID
                                string optionName = string.Empty;
                                foreach (var opt in json.options ?? Enumerable.Empty<dynamic>())
                                {
                                    foreach (var value in opt.values ?? Enumerable.Empty<dynamic>())
                                    {
                                        if (value?.id != null && value.id.ToString() == optionValue)
                                        {
                                            optionName = value.name?.ToString() ?? optionValue;
                                            break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(optionName)) break;
                                }

                                variant.Attributes.Add(new Entity.Attribute
                                {
                                    Name = Product.Attributes[i - 1]?.Name ?? $"Option{i}",
                                    Value = optionName
                                });
                            }
                        }
                    }

                    Product.Variants.Add(variant);
                }
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error in CrawlShopbase: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public void CrawlOnePage()
        {
            var content = driver.FindElement(By.Id("__NEXT_DATA__")).GetAttribute("innerHTML");
            var raw = JsonConvert.DeserializeObject<dynamic>(content).props.pageProps.data;
            var json = raw.product;

            Product = new Product()
            {
                Title = json.title.ToString(),
                Description = raw.settings.pages.middleDes.ToString()
            };

            foreach (var img in json.images)
                Product.Images.Add(new Image { Src = img.ToString() });

            JObject props = JObject.Parse(JsonConvert.SerializeObject(json.properties));
            var attrNameList = props.Properties().Select(c => c.Name).ToList();
            foreach (var attrName in attrNameList)
            {
                Product.Attributes.Add(new Entity.Attribute
                {
                    Name = attrName.ToString(),
                    Values = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(json.properties[attrName.ToString()]))
                });
            }

            foreach (var item in json.variants)
            {
                if ((bool)item.allow_to_sell)
                {
                    var variant = new Variant()
                    {
                        Image = item.image_url.ToString(),
                        Price = (float)item.default_price,
                        RegularPrice = (float)item.compared_price
                    };

                    foreach (var attrName in attrNameList)
                    {
                        variant.Attributes.Add(new Entity.Attribute
                        {
                            Name = attrName.ToString(),
                            Value = item.properties[attrName.ToString()].ToString()
                        });
                    }

                    Product.Variants.Add(variant);
                }
            }
        }

        public void CrawlSelless()
        {
            var content = driver.FindElement(By.Id("__NEXT_DATA__")).GetAttribute("innerHTML");
            var uri = new Uri(driver.Url);
            var raw = JsonConvert.DeserializeObject<dynamic>(content).props.pageProps.store.pages;
            var slug = uri.AbsolutePath.Replace("/", string.Empty);

            if (raw[slug] != null)
            {
                var json = raw[slug].setting;
                Product = new Product()
                {
                    Title = json.settings.general.title.ToString(),
                    Description = json.parts.content.middle.ToString()
                };

                foreach (var img in json.gallery)
                    Product.Images.Add(new Image { Src = $"https://cdn.selless.us/{img}" });

                JObject props = JObject.Parse(JsonConvert.SerializeObject(json.variants.properties));
                var attrNameList = props.Properties().Select(c => c.Name).ToList();

                foreach (var attrName in attrNameList)
                {
                    var values = props[attrName.ToString()]["values"];
                    JObject _values = JObject.Parse(JsonConvert.SerializeObject(values));
                    Product.Attributes.Add(new Entity.Attribute
                    {
                        Name = attrName.ToString(),
                        Values = _values.Properties().Select(c => c.Name).ToList()
                    });
                }

                #region Variations
                JObject variationObj = JObject.Parse(JsonConvert.SerializeObject(json.variants.items));
                foreach (JToken child in variationObj.Children())
                {
                    if ((bool)child.First()["sellable"])
                    {
                        var variant = new Variant()
                        {
                            Image = $"https://cdn.selless.us/{child.First()["logo"]}",
                            Price = (float)child.First()["default_price"],
                            RegularPrice = (float)child.First()["compare_price"]
                        };

                        foreach (var attrName in attrNameList)
                        {
                            variant.Attributes.Add(new Entity.Attribute
                            {
                                Name = attrName.ToString(),
                                Value = child.First()["properties"][attrName.ToString()].ToString()
                            });
                        }

                        Product.Variants.Add(variant);
                    }
                }
                #endregion
            }
        }

        public void CrawlWoo()
        {
            Product = new Product
            {
                Title = driver.FindElement(By.TagName("title")).GetAttribute("innerHTML")
            };

            if (WaitUntilElementExists(By.XPath("//table[@class='variations']")))
            {
                var table = driver.FindElement(By.XPath("//table[@class='variations']"));
                foreach (var tr in table.FindElements(By.TagName("tr")))
                {
                    var label = tr.FindElements(By.TagName("label"))[0];
                    var attr = new Entity.Attribute
                    {
                        Name = label.Text
                    };

                    var id = label.GetAttribute("for");
                    var select = tr.FindElement(By.Id(id));
                    foreach (var opt in select.FindElements(By.TagName("option")))
                    {
                        var value = opt.GetAttribute("value");
                        if (!string.IsNullOrEmpty(value))
                            attr.Values.Add(value);
                    }

                    attr.Slug = select.GetAttribute("name");
                    Product.Attributes.Add(attr);
                }
            }

            #region Gallery
            if (WaitUntilElementExists(By.XPath("//img[@class='attachment-woocommerce_thumbnail']")))
            {
                var imgList = driver.FindElements(By.XPath("//img[@class='attachment-woocommerce_thumbnail']"));
                foreach (var img in imgList)
                {
                    var src = img.GetAttribute("src");
                    src = Regex.Replace(src, "-([0-9]+)x([0-9]+)", string.Empty);
                    Product.Images.Add(new Image { Src = src });
                }

                Product.Images = Product.Images.Distinct().ToList();
            }
            #endregion

            if (WaitUntilElementExists(By.XPath("//form[@data-product_variations]")))
            {
                var form = driver.FindElement(By.XPath("//form[@data-product_variations]"));
                if (form.GetAttribute("class").Contains("variations_form "))
                {
                    if (!string.IsNullOrEmpty(form.GetAttribute("data-product_variations")))
                    {
                        var json = JsonConvert.DeserializeObject<dynamic>(form.GetAttribute("data-product_variations"));
                        foreach (var item in json)
                        {
                            var variant = new Variant
                            {
                                Price = (float)item.display_price,
                                RegularPrice = (float)item.display_regular_price,
                                Image = item.image.full_src.ToString()
                            };

                            foreach (var att in Product.Attributes)
                            {
                                variant.Attributes.Add(new Entity.Attribute
                                {
                                    Name = att.Name,
                                    Value = item.attributes[att.Slug].ToString()
                                });
                            }

                            if (Product.Images.Count == 0)
                                Product.Images.Add(new Image { Src = item.image.full_src.ToString() });
                            Product.Variants.Add(variant);
                        }
                    }
                }
            }

            if (WaitUntilElementExists(By.XPath("//div[@data-product-description]")))
                Product.Description = driver.FindElement(By.XPath("//div[@data-product-description]")).GetAttribute("innerHTML");
        }

        private bool WaitUntilElementExists(By elementLocator, int timeout = 5)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                wait.Until(ExpectedConditions.ElementIsVisible(elementLocator));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            try
            {
                driver.Close();
            }
            catch { }

            try
            {
                driver.Quit();
            }
            catch { }

            driver.Dispose();
        }
    }
}
