using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using MonitoramentoSites.Models;

namespace MonitoramentoSites.Clients
{
    public class CanalSlackClient
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
 
        public CanalSlackClient(HttpClient client,
            IConfiguration configuration)
        {
            _client = client;
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            _configuration = configuration;
        }

        public void PostAlerta(ResultadoMonitoramento resultado)
        {
            var respLogicApp = _client.PostAsJsonAsync<ResultadoMonitoramento>(
                _configuration["UrlLogicAppSlack"], resultado).Result;
            respLogicApp.EnsureSuccessStatusCode();
        }
    }
}