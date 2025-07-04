using Asiup_Clone_Product.Lib;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v2;
using IniParser;
using IniParser.Model;
using Asiup_Clone_Product.Entity;
using System.IO;
using Product = WooCommerceNET.WooCommerce.v2.Product;
using Variation = WooCommerceNET.WooCommerce.v2.Variation;
using Amib.Threading;
using System.Threading;

namespace Asiup_Clone_Product
{
    internal class Program
    {
        static readonly List<Crawler> CrawlerList = new List<Crawler>();
        static RestAPI restAPI;
        static WCObject wcObj;
        static readonly Config config = new Config();
        static SmartThreadPool crawlPool;
        static SmartThreadPool restApiPool;
        static Queue<Entity.Product> productsQueue;

        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            #region Init Data
            crawlPool = new SmartThreadPool(new STPStartInfo
            {
                MinWorkerThreads = 3,
                MaxWorkerThreads = 5
            });

            restApiPool = new SmartThreadPool(new STPStartInfo
            {
                MinWorkerThreads = 3,
                MaxWorkerThreads = 8,
                MaxQueueLength = 8,
                AreThreadsBackground = true
            });

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Directory.GetCurrentDirectory()}\\config.ini");

            config.SiteUrl = data["store"]["url"];
            config.SiteApiSecret = data["store"]["secret"];
            config.SiteApiKey = data["store"]["key"];

            restAPI = new RestAPI($"{config.SiteUrl}/wp-json/wc/v2/", config.SiteApiKey, config.SiteApiSecret);
            wcObj = new WCObject(restAPI);
            productsQueue = new Queue<Entity.Product>();
            #endregion

            //new Thread(PushProductAsync).Start();

            var list = File.ReadAllLines($"{Directory.GetCurrentDirectory()}\\list.txt").ToList();
            foreach (var item in list)
            {
                try
                {
                    var crawler = new Crawler(item);
                    Console.WriteLine($"+ Crawling {item}");
                    CrawlerList.Add(crawler);
                    var idx = CrawlerList.FindIndex(c => c.Id == crawler.Id);

                    crawler.DetectPlatform();

                    if (crawler.Platform == Platform.SHOPIFY)
                    {
                        crawler.CrawlShopify();
                    }
                    else if (crawler.Platform == Platform.SELLESS)
                    {
                        crawler.CrawlSelless();
                    }
                    else if (crawler.Platform == Platform.ONE_PAGE)
                    {
                        crawler.CrawlOnePage();
                    }
                    else if (crawler.Platform == Platform.WOOCOMMERCE)
                    {
                        crawler.CrawlWoo();
                    }
                    else if (crawler.Platform == Platform.SHOPBASE)
                    {
                        crawler.CrawlShopbase();
                    }

                    crawler.Close();
                    await PushProduct(crawler.Product);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }
            }

            restApiPool.WaitForIdle();
        }

        private static async void PushProductAsync()
        {
            while (true)
            {
                if (productsQueue.Count > 0)
                {
                    try
                    {
                        var product = productsQueue.Dequeue();
                        PushProduct(product);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                Thread.Sleep(2000);
            }
        }

        static async Task PushProduct(Entity.Product entityProduct)
        {
            if (entityProduct == null)
                throw new ArgumentNullException(nameof(entityProduct));

            try
            {
                // Map Entity.Product to WooCommerce Product
                var wcProduct = new Product
                {
                    type = "variable",
                    name = entityProduct.Title ?? string.Empty,
                    enable_html_description = true,
                    description = entityProduct.Description ?? string.Empty,
                    in_stock = true,
                    images = entityProduct.Images?
                        .Where(img => img?.Src != null)
                        .Select(img => new ProductImage { src = img.Src })
                        .ToList() ?? new List<ProductImage>(),
                    attributes = entityProduct.Attributes?
                        .Select((att, index) => new ProductAttributeLine
                        {
                            name = att.Name ?? $"Attribute{index + 1}",
                            options = att.Values ?? new List<string>(),
                            visible = true,
                            variation = true,
                            position = index + 1
                        })
                        .ToList() ?? new List<ProductAttributeLine>(),
                    meta_data = new List<ProductMeta>
                    {
                        new ProductMeta { key = "_enable_ec_button", value = "yes" }
                    }
                };

                // Add product to WooCommerce
                wcProduct = await wcObj.Product.Add(wcProduct);
                if (wcProduct?.id == null)
                    throw new InvalidOperationException("Failed to create product in WooCommerce.");

                // Add variants
                var variantTasks = entityProduct.Variants?
                    .Where(v => v != null)
                    .Select(async item =>
                    {
                        var variant = new Variation
                        {
                            price = AdjustPrice(item.Price),
                            regular_price = AdjustPrice(item.RegularPrice),
                            sale_price = AdjustPrice(item.Price),
                            attributes = item.Attributes?
                                .Where(att => att?.Name != null && att.Value != null)
                                .Select(att => new VariationAttribute
                                {
                                    name = att.Name,
                                    option = att.Value
                                })
                                .ToList() ?? new List<VariationAttribute>(),
                            image = item.Image != null ? new VariationImage { src = item.Image } : null,
                            in_stock = true
                        };

                        await wcObj.Product.Variations.Add(variant, (ulong)wcProduct.id);
                    })
                    .ToList() ?? new List<Task>();

                await Task.WhenAll(variantTasks);

                Console.WriteLine($"- Done {wcProduct.permalink} - {wcProduct.name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pushing product '{entityProduct.Title}': {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        static decimal AdjustPrice(float price)
        {
            return (decimal)(price > 1000 ? price / 10000 : price * 3.5);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var crawler in CrawlerList)
            {
                crawler.Close();
            }

            foreach (var f in Process.GetProcessesByName("chromedriver"))
                f.Kill();
        }
    }
}
