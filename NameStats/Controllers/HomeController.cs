using Microsoft.AspNetCore.Mvc;
using NameStats.Models;
using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace NameStats.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        [HttpGet("/get-data")]
        public async Task<IActionResult> GetData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter is required.");

            // Convert name to uppercase and build URL path
            name = name.ToUpperInvariant();
            var firstLetter = name.Length > 0 ? name[0].ToString() : "A";
            var firstTwoLetters = name.Length > 1 ? name.Substring(0, 2) : "AB";

            var url = $"https://www.mynamestats.com/First-Names/{firstLetter}/{firstTwoLetters}/{name}/index.html";

            try
            {
                using var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var nameHeader = htmlDoc.DocumentNode
                    .SelectSingleNode("//div[@class='table-stats']/h3")
                    ?.InnerText.Replace("First Names", "").Replace("National Statistics", "").Trim();

                var ulElements = htmlDoc.DocumentNode.SelectNodes("//div[@class='table-stats']/ul");

                var myNameStats = new Dictionary<string, string>();
                var ssaStats = new Dictionary<string, string>();

                if (ulElements?.Count >= 2)
                {
                    var statList1 = ulElements[0].SelectNodes(".//li");
                    var statList2 = ulElements[1].SelectNodes(".//li");

                    foreach (var li in statList1 ?? Enumerable.Empty<HtmlNode>())
                    {
                        var key = li.SelectSingleNode(".//span[@class='span-lft']")?.InnerText.Trim();
                        var rawValue = li.SelectSingleNode(".//span[@class='span-rgt']")?.InnerText.Trim();

                        if (key != null && rawValue != null)
                        {
                            // Clean "Total Population" field to remove "+/-..." part
                            if (key.Contains("Population", StringComparison.OrdinalIgnoreCase))
                            {
                                // Remove anything after a space followed by a '+' or '-' character
                                rawValue = Regex.Replace(rawValue, @"\s[+/-].*$", "").Trim();
                            }

                            myNameStats[key] = rawValue;
                        }
                    }

                    foreach (var li in statList2 ?? Enumerable.Empty<HtmlNode>())
                    {
                        var key = li.SelectSingleNode(".//span[@class='span-lft']")?.InnerText.Trim();
                        var value = li.SelectSingleNode(".//span[@class='span-rgt']")?.InnerText.Trim();

                        if (key != null && value != null)
                        {
                            ssaStats[key] = value;
                        }
                    }
                }

                return Json(new
                {
                    Name = nameHeader,
                    MyNameStats = myNameStats,
                    SsaStats = ssaStats
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Failed to fetch data from external site.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
