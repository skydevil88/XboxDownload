using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    internal class Market
    {
        public String ename;
        public String cname;
        public String code;
        public String language;

        public Market(String ename, String cname, String code, String language)
        {
            this.cname = cname;
            this.code = code;
            this.language = language;
            this.ename = ename;
        }

        public override string ToString()
        {
            return this.cname;
        }
    }

    internal class Product
    {
        public String title;
        public String id;

        public Product(String title, string id)
        {
            this.title = title;
            this.id = id;
        }

        public override string ToString()
        {
            return this.title;
        }
    }

    internal class ClassGame
    {
        public class Game
        {
            public List<Products> Products { get; set; } = new List<Products>();
        }

        public class Products
        {
            public DateTime LastModifiedDate { get; set; }
            public List<LocalizedProperties> LocalizedProperties { get; set; } = new List<LocalizedProperties>();
            public List<MarketProperties> MarketProperties { get; set; } = new List<MarketProperties>();
            public Properties Properties { get; set; } = new Properties();
            public List<DisplaySkuAvailabilities> DisplaySkuAvailabilities { get; set; } = new List<DisplaySkuAvailabilities>();
            public string ProductId { get; set; } = "";
        }

        public class LocalizedProperties
        {
            public string DeveloperName { get; set; } = "";
            public string PublisherName { get; set; } = "";
            public EligibilityProperties EligibilityProperties { get; set; } = new EligibilityProperties();
            public List<Images> Images { get; set; } = new List<Images>();
            public string ProductDescription { get; set; } = "";
            public string ProductTitle { get; set; } = "";
            public string[] Markets { get; set; } = Array.Empty<string>();
        }

        public class MarketProperties
        {
            public DateTime OriginalReleaseDate { get; set; }
        }

        public class EligibilityProperties
        {
            public Affirmations[] Affirmations { get; set; } = Array.Empty<Affirmations>();
        }

        public class Affirmations
        {
            public string Description { get; set; } = "";
        }

        public class Images
        {
            public string ImagePurpose { get; set; } = "";
            public string Uri { get; set; } = "";
            public int Height { get; set; }
            public int Width { get; set; }
        }

        public class DisplaySkuAvailabilities
        {
            public Sku Sku { get; set; } = new Sku();
            public List<Availabilities> Availabilities { get; set; } = new List<Availabilities>();
        }

        public class Sku
        {
            public Properties Properties { get; set; } = new Properties();
            public string SkuType { get; set; } = "";
        }

        public class Properties
        {
            public string Category { get; set; } = "";
            public List<Packages> Packages { get; set; } = new List<Packages>();
            public List<BundledSkus> BundledSkus { get; set; } = new List<BundledSkus>();

            //EA Play
            public string[] MerchandisingTags { get; set; } = Array.Empty<string>();
        }

        public class Packages
        {
            public ulong MaxDownloadSizeInBytes { get; set; }
            public string[] Languages { get; set; } = Array.Empty<string>();
            public string PackageFormat { get; set; } = "";
            public string PackageFullName { get; set; } = "";
            public string ContentId { get; set; } = "";
            public int PackageRank { get; set; }
            public List<PlatformDependencies> PlatformDependencies { get; set; } = new List<PlatformDependencies>();
            public List<PackageDownloadUris> PackageDownloadUris { get; set; } = new List<PackageDownloadUris>();
            public FulfillmentData FulfillmentData { get; set; } = new FulfillmentData();
        }

        public class BundledSkus
        {
            public string BigId { get; set; } = "";
        }

        public class PlatformDependencies
        {
            public string PlatformName { get; set; } = "";
        }

        public class PackageDownloadUris
        {
            public string Uri { get; set; } = "";
        }

        public class FulfillmentData
        {
            public string WuCategoryId { get; set; } = "";
        }


        public class Availabilities
        {
            public Conditions Conditions { get; set; } = new Conditions();
            public OrderManagementData OrderManagementData { get; set; } = new OrderManagementData();
            public Properties Properties { get; set; } = new Properties();
        }

        public class Conditions
        {
            public DateTime EndDate { get; set; }
            public DateTime StartDate { get; set; }
        }
        public class OrderManagementData
        {
            public Price Price { get; set; } = new Price();
        }

        public class Price
        {
            public string CurrencyCode { get; set; } = "";
            public double MSRP { get; set; }
            public double ListPrice { get; set; }
            public double WholesalePrice { get; set; }
        }

        //Search
        public class Search
        {
            public string Query { get; set; } = "";
            public List<ResultSets> ResultSets { get; set; } = new List<ResultSets>();
        }

        public class ResultSets
        {
            public List<Suggests> Suggests { get; set; } = new List<Suggests>();
        }

        public class Suggests
        {
            public string Source { get; set; } = "";
            public string Title { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public List<Metas> Metas { get; set; } = new List<Metas>();
        }
        public class Metas
        {
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
        }

       public static void ExchangeRate(string CurrencyCode)
        {
            if (CurrencyCode == "CNY") return;
            string html;
            Match result;
            
            html = ClassWeb.HttpResponseContent("https://hq.sinajs.cn/list=fx_s" + CurrencyCode.ToLowerInvariant() + "cny", "GET", null, null, new() { { "Referer", "https://finance.sina.com.cn/" } });
            result = Regex.Match(html, @"\d{2}:\d{2}:\d{2}(,[^,]*){7},(?<ExchangeRate>[^,]+)");
            if (result.Success)
            {
                if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                {
                    Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                    return;
                }
            }
            html = ClassWeb.HttpResponseContent("https://www.majorexchangerates.com/" + CurrencyCode.ToLowerInvariant() + "/cny.html");
            result = Regex.Match(html, @"<a [^>]+>1 " + CurrencyCode + " = (?<ExchangeRate>.+) CNY</a>");
            if (result.Success)
            {
                if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                {
                    Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                    return;
                }
            }
            
            html = ClassWeb.HttpResponseContent("https://www.convertworld.com/zh-hans/currency/mauritania/" + CurrencyCode.ToLowerInvariant() + "-cny.html");
            result = Regex.Match(html, @"一个" + CurrencyCode + "是(?<ExchangeRate>.+) CNY");
            if (result.Success)
            {
                if (double.TryParse(result.Groups["ExchangeRate"].Value, out double ExchangeRate))
                {
                    Form1.dicExchangeRate.AddOrUpdate(CurrencyCode, ExchangeRate, (oldkey, oldvalue) => ExchangeRate);
                    return;
                }
            }
        }
    }

    internal class XboxGameDownload
    {
        public static ConcurrentDictionary<String, Products> dicXboxGame = new();

        static int delay = 0;
        public static void SaveXboxGame()
        {
            if (delay >= 1)
            {
                delay = 6;
                return;
            }
            Task.Run(() =>
            {
                delay = 6;
                while (delay >= 1)
                {
                    delay--;
                    Thread.Sleep(1000);
                }
                XboxGame xboxGame = new()
                {
                    Serialize = dicXboxGame
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(xboxGame, options);
                try
                {
                    if (!Directory.Exists(Form1.resourcePath))
                        Directory.CreateDirectory(Form1.resourcePath);
                    File.WriteAllText(Form1.resourcePath + "\\XboxGame.json", jsonString);
                }
                catch { }
            });
        }

        public class XboxGame
        {
            public ConcurrentDictionary<String, Products>? Serialize { get; set; }
        }

        public class Products
        {
            public Version Version { get; set; } = new Version();
            public ulong FileSize { get; set; }
            public string Url { get; set; } = "";
        }

        public class Game
        {
            public bool PackageFound { get; set; }
            public string ContentId { get; set; } = "";
            public List<PackageFiles> PackageFiles { get; set; } = new List<PackageFiles>();
        }

        public class PackageFiles
        {
            public ulong FileSize { get; set; }
            public string[] CdnRootPaths { get; set; } = Array.Empty<string>();
            public string RelativeUrl { get; set; } = "";
            public DateTime ModifiedDate { get; set; }
        }
    }

    internal class XboxPackage
    {
        public class Game
        {
            public String Code { get; set; } = "";
            public Data Data { get; set; } = new Data();
        }

        public class App
        {
            public String Code { get; set; } = "";
            public List<Data> Data { get; set; } = new List<Data>();
        }

        public class Data
        {
            public String Name { get; set; } = "";
            public ulong Size { get; set; }
            public String Url { get; set; } = "";
        }
    }

    internal class PsGame
    {
        public class Game
        {
            public long OriginalFileSize { get; set; }
            public int NumberOfSplitFiles { get; set; }
            public List<Pieces> Pieces { get; set; } = new List<Pieces>();
        }

        public class Pieces
        {
            public string Url { get; set; } = "";
        }
    }
}
