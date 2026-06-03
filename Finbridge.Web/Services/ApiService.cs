using System.Net.Http.Json;
using Finbridge.Web.ViewModels;

namespace Finbridge.Web.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _settings;

        public ApiService(HttpClient httpClient, ApiSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<List<UserViewModel>> GetUsersAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<UserViewModel>>($"{_settings.BaseUrl}/api/users");
            return response ?? new List<UserViewModel>();
        }

        public async Task<List<BalanceHistoryViewModel>> GetBalanceHistoryAsync(int userId)
        {
            var response = await _httpClient.GetFromJsonAsync<List<BalanceHistoryViewModel>>($"{_settings.BaseUrl}/api/balances/history/{userId}");
            return response ?? new List<BalanceHistoryViewModel>();
        }
    }
}