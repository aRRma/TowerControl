using System.Text;
using TowerControl.ApiModels.TowerControlToGroundControl;
using TowerControl.Helpers;

namespace TowerControl.Services
{
    public class GroundControlCommandService
    {
        // вот тут надо задать URL сервиса диспетчера наземных служб
        // примерно так "http://192.168.1.1:8000"
        private static string _baseAddres = "http://localhost";
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public GroundControlCommandService(ILogger<GroundControlCommandService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _client = httpClientFactory.CreateClient("client") ?? new HttpClient();
        }

        public async Task<bool> AddPlane(GroundControlPlaneAdd request, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/groundControl/planes/add");
            _client.DefaultRequestHeaders.Clear();

            try
            {
                var data = new XmlSerializerHelper<GroundControlPlaneAdd>().StringSerialize(request);
                var content = new StringContent(data, Encoding.UTF8, "application/xml");
                var result = await _client.PostAsync(_client.BaseAddress, content);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when try add plane to ground control");
                return false;
            }
        }
    }
}
