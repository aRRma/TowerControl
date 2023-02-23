using System.Text;
using TowerControl.ApiModels.TowerControlToApproachControl;
using TowerControl.Helpers;

namespace TowerControl.Services
{
    public class ApproachControlCommandService
    {
        // вот тут надо задать URL сервиса диспетчера подлета
        // примерно так "http://192.168.1.1:8000"
        private static string _baseAddres = "http://localhost";
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public ApproachControlCommandService(ILogger<ApproachControlCommandService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _client = httpClientFactory.CreateClient("client") ?? new HttpClient();
        }

        public async Task<bool> AddPlane(ApproachControlPlaneAdd request, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/approachControl/planes/add");
            _client.DefaultRequestHeaders.Clear();

            try
            {
                var data = new XmlSerializerHelper<ApproachControlPlaneAdd>().StringSerialize(request);
                var content = new StringContent(data, Encoding.UTF8, "application/xml");
                var result = await _client.PostAsync(_client.BaseAddress, content);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when try add plane to approach control");
                return false;
            }
        }
    }
}
