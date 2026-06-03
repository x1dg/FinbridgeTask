using Microsoft.AspNetCore.Mvc;
using Finbridge.Web.Services;
using Finbridge.Web.ViewModels;
using Finbridge.Web.Models;
using System.Diagnostics;

namespace Finbridge.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;

        public HomeController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _apiService.GetUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> BalanceHistory(int id)
        {
            var history = await _apiService.GetBalanceHistoryAsync(id);
            return View(history);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}