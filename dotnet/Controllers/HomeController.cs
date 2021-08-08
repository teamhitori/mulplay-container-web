using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Models;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            HttpClient httpClient,
            ILogger<HomeController> logger, 
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Editor");
            }
            else
            {
                return View();
            }
        }

        //[HttpGet("{gameName}")]
        //public IActionResult Index(string gameName)
        //{
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        return View("Editor");
        //    }
        //    else
        //    {
        //        return View();
        //    }
        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("__test__")]
        public async Task<string> test(string gameName)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send("mulplay-container-game", timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("Address: {0}", reply.Address.ToString());
                _logger.LogInformation("RoundTrip time: {0}", reply.RoundtripTime);
                _logger.LogInformation("Time to live: {0}", reply.Options.Ttl);
                _logger.LogInformation("Don't fragment: {0}", reply.Options.DontFragment);
                _logger.LogInformation("Buffer size: {0}", reply.Buffer.Length);
            }

            var uri = "http://mulplay-container-game";
            var responsePost = await _httpClient.GetAsync(uri);
            var resp = await responsePost.Content.ReadAsStringAsync();
            return resp;
        }
    }
}
