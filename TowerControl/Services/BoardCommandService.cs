using System.Runtime.CompilerServices;
using System.Text;
using TowerControl.ApiModels.Base;
using TowerControl.ApiModels.TowerControlToBoard;
using TowerControl.Data;
using TowerControl.Helpers;

namespace TowerControl.Services
{
    public class BoardCommandService
    {
        // вот тут надо задать URL сервиса самолета
        // примерно так "http://192.168.1.1:8000"
        private static string _baseAddres = "http://localhost";
        private readonly HttpClient _client;
        private readonly AppDbContext _db;
        private readonly ILogger _logger;

        public BoardCommandService(ILogger<BoardCommandService> logger, IHttpClientFactory httpClientFactory, AppDbContext db)
        {
            _logger = logger;
            _client = httpClientFactory.CreateClient("client") ?? new HttpClient();
            _db = db;
        }

        public async Task<BoardGetCoords?> GetBoardCoordinates(string boardNumber, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/board/{boardNumber}/coords");
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Accept", "application/xml");

            try
            {
                var result = await _client.GetAsync(_client.BaseAddress, ct);
                if (result.IsSuccessStatusCode)
                {
                    var data = await result.Content.ReadAsStringAsync(ct);
                    return new XmlSerializerHelper<BoardGetCoords>().Deserialize(data);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error when try get board coordinates");
                return null;
            }
        }

        public async Task<bool> AssignBoardFlyTo(string boardNumber, BoardFlyTo request, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/board/{boardNumber}/flyTo");
            return await PostOnBoard(request, ct);
        }

        public async Task<bool> SendBoardOnCircuit(string boardNumber, BoardOnCircuit request, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/board/{boardNumber}/flyTo");
            return await PostOnBoard(request, ct);
        }

        public async Task<bool> ChangeBoardSpeedOnCircuit(string boardNumber, BoardOnCircuit request, CancellationToken ct = default)
        {
            _client.BaseAddress = new Uri($"{_baseAddres}/board/{boardNumber}/circuitSpeed");
            return await PostOnBoard(request, ct);
        }

        private async Task<bool> PostOnBoard<TReq>(TReq request, CancellationToken ct = default, [CallerMemberName] string method = "")
            where TReq : IBaseRequest
        {
            _client.DefaultRequestHeaders.Clear();

            try
            {
                var data = new XmlSerializerHelper<TReq>().StringSerialize(request);
                var content = new StringContent(data, Encoding.UTF8, "application/xml");
                var result = await _client.PostAsync(_client.BaseAddress, content);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when try {method}");
                return false;
            }
        }
    }
}
