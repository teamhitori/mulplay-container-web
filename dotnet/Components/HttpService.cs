using Azure.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Polly;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly Random _jitterer;

        public HttpService(
            IWebHostEnvironment env,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _jitterer = new Random();
            var scopeDefault = _configuration["Scope_Default"];
            _httpClient.Timeout = TimeSpan.FromSeconds(140);
        }

        public async Task<T> UrlDeleteType<T>(string uri, int retries = 3) where T : class
        {
            try
            {
                var uriBuilder = new UriBuilder(uri);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                uriBuilder.Query = query.ToString();
                uri = uriBuilder.ToString();
                var res = await Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(retries, (retryAttempt, timespan) =>
                    {
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt * 2))
                                  + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                    }, (ex, timespan, retryCount, context) =>
                    {
                        _logger.LogError(ex, $"UrlDeleteType { ex.Message }, retry: {retryCount}, timespan: {timespan}");
                    })
                    .ExecuteAsync(async () =>
                    {
                        var responsePost = await _httpClient.DeleteAsync(uri);
                        if (!responsePost.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException(uri);
                        }
                        var json = await responsePost.Content.ReadAsStringAsync();
                        return json.GetObject<T>();
                    });

                return res;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"UrlDeleteType { ex.Message }, final Exception");
                return null;
            }
            
        }

        public async Task<T> UrlGetType<T>(string uri, int retries = 3) where T : class
        {
            try
            {
                var uriBuilder = new UriBuilder(uri);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                uriBuilder.Query = query.ToString();
                uri = uriBuilder.ToString();
                var res = await Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(retries, (retryAttempt, timespan) =>
                    {
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt * 2))
                                  + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                    }, (ex, timespan, retryCount, context) =>
                    {
                        _logger.LogError(ex, $"UrlGetType { ex.Message }, retry: {retryCount}, timespan: {timespan}");
                    })
                    .ExecuteAsync(async () =>
                    {
                        var responsePost = await _httpClient.GetAsync(uri);
                        if (!responsePost.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException(uri);
                        }
                        var json = await responsePost.Content.ReadAsStringAsync();
                        return json.GetObject<T>();
                    });

                return res;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"UrlGetType { ex.Message }, final Exception");
                return null;
            }
            
        }

        public async Task<T> UrlPostType<T>(string uri, string postContent, int retries = 6) where T : class
        {
            try
            {
                var uriBuilder = new UriBuilder(uri);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                uriBuilder.Query = query.ToString();
                uri = uriBuilder.ToString();
                var stringContent = new StringContent(postContent, Encoding.UTF8, "application/json");
                var res = await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(retries, (retryAttempt, timespan) =>
                   {
                       return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                 + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                   }, (ex, timespan, retryCount, context) =>
                   {
                       _logger.LogError(ex, $"UrlPostType { ex.Message }, retry: {retryCount}, timespan: {timespan}");
                   })
                   .ExecuteAsync(async () =>
                   {
                       var responsePost = await _httpClient.PostAsync(uri, stringContent);
                       if (!responsePost.IsSuccessStatusCode)
                       {
                           throw new HttpRequestException(uri);
                       }
                       var json = await responsePost.Content.ReadAsStringAsync();
                       return json.GetObject<T>();
                   });

                return res;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, $"UrlPostType { ex.Message }, final Exception");
                return null;
            }
        }
    }
}
