using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace TextExtraction.Services
{
    public class APIService : IAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<APIService> logger;

        public APIService(HttpClient httpClient, ILogger<APIService> logger)
        {
            _httpClient = httpClient;
            this.logger = logger;
        }
        public async Task<TResponse?> GetAsync<TResponse>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response is not null && response.StatusCode.Equals(HttpStatusCode.OK))
                return await response!.Content!.ReadFromJsonAsync<TResponse>();
            logger.LogError("Getting error while calling {url}", url);
            return default;
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest requestData)
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestData);
            if (response is not null && response.IsSuccessStatusCode)
                return await response!.Content!.ReadFromJsonAsync<TResponse>();
            logger.LogError("Getting error while calling {url}", url);
            return default;
        }
    }
}
