using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoramentoSites.Clients;
using MonitoramentoSites.Models;

namespace MonitoramentoSites
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        public readonly CanalSlackClient _canalSlackClient;

        public Worker(ILogger<Worker> logger,
            IConfiguration configuration,
            CanalSlackClient canalSlackClient)
        {
            _logger = logger;
            _configuration = configuration;
            _canalSlackClient = canalSlackClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"MonitoramentoSites - iniciando execução em: {DateTime.Now}");


            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var sites = _configuration["Sites"]
                    .Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (string site in sites)
                {
                    var dadosLog = new ResultadoMonitoramento();
                    dadosLog.site = site;

                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(site);
                        client.DefaultRequestHeaders.Accept.Clear();

                        try
                        {
                            // Envio da requisicao a fim de determinar se
                            // o site esta no ar
                            HttpResponseMessage response =
                                client.GetAsync("").Result;

                            dadosLog.status = (int)response.StatusCode + " " +
                                response.StatusCode;
                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                dadosLog.descricaoErro = response.ReasonPhrase;
                        }
                        catch (Exception ex)
                        {
                            dadosLog.status = "Exception";
                            dadosLog.descricaoErro = ex.Message;
                        }
                    }

                    string jsonResultado =
                        JsonSerializer.Serialize(dadosLog);

                    if (dadosLog.descricaoErro == null)
                        _logger.LogInformation(jsonResultado);
                    else
                    {
                        _logger.LogError(jsonResultado);
                        _canalSlackClient.PostAlerta(dadosLog);
                    }
                }

                await Task.Delay(Convert.ToInt32(
                    _configuration["IntervaloExecucao"]), stoppingToken);
            }
        }
    }
}