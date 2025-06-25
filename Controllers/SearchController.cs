// Controllers/SearchController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;
using TaskManager.Web.ViewModels.Search;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly IElasticsearchService _elasticsearchService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IElasticsearchService elasticsearchService,
            UserManager<ApplicationUser> userManager,
            ILogger<SearchController> logger)
        {
            _elasticsearchService = elasticsearchService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(SearchViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;

            if (!string.IsNullOrEmpty(model.Query))
            {
                var filters = new TaskSearchFilters
                {
                    Status = model.SelectedStatuses,
                    Priority = model.SelectedPriorities,
                    ProjectId = model.ProjectId,
                    IsOverdue = model.ShowOverdueOnly ? true : null
                };

                var searchResult = await _elasticsearchService.SearchTasksAsync(
                    model.Query, 
                    userId, 
                    model.Page, 
                    model.PageSize,
                    filters);

                model.Results = searchResult.Documents;
                model.TotalResults = searchResult.Total;
                model.SearchTime = searchResult.TookMilliseconds;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Suggestions(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
                return Json(new List<string>());

            var userId = _userManager.GetUserId(User)!;
            var suggestions = await _elasticsearchService.GetSearchSuggestionsAsync(query, userId);
            
            return Json(suggestions);
        }

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            var userId = _userManager.GetUserId(User)!;
            
            var statusAgg = await _elasticsearchService.GetTaskStatusAggregationAsync(userId);
            var priorityAgg = await _elasticsearchService.GetTaskPriorityAggregationAsync(userId);
            var overdueTasks = await _elasticsearchService.GetOverdueTasksAsync(userId);

            var viewModel = new SearchAnalyticsViewModel
            {
                StatusDistribution = statusAgg,
                PriorityDistribution = priorityAgg,
                OverdueTasks = overdueTasks,
                TotalOverdue = overdueTasks.Count
            };

            return View(viewModel);
        }
    }
}