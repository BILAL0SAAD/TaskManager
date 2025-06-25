using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.Models;
using TaskManager.Web.Services.Interfaces;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    [Route("api/debug/elasticsearch")]
    public class ElasticsearchDebugController : Controller
    {
        private readonly IElasticsearchService _elasticsearchService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ElasticsearchDebugController> _logger;

        public ElasticsearchDebugController(
            IElasticsearchService elasticsearchService,  // FIXED: Use interface
            UserManager<ApplicationUser> userManager,
            ILogger<ElasticsearchDebugController> logger)
        {
            _elasticsearchService = elasticsearchService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var connected = await _elasticsearchService.TestConnectionAsync();
                return Json(new { connected, message = connected ? "Connected to Elasticsearch" : "Failed to connect" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("check-index")]
        public async Task<IActionResult> CheckIndex()
        {
            try
            {
                var indexExists = await _elasticsearchService.IndexExistsAsync();
                return Json(new { indexExists, message = indexExists ? "Index exists" : "Index does not exist" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("get-all-documents")]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var documents = await _elasticsearchService.GetAllTasksForDebuggingAsync(userId);
                
                return Json(new 
                { 
                    count = documents.Count,
                    userId = userId,
                    documents = documents.Take(5).Select(d => new 
                    {
                        d.Id,
                        d.Title,
                        d.Description,
                        d.Status,
                        d.Priority,
                        d.UserId,
                        d.IsDeleted,
                        d.SearchContent
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("test-document-exists")]
        public async Task<IActionResult> TestDocumentExists()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var result = await _elasticsearchService.TestDocumentExistsAsync(userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("get-mapping")]
        public async Task<IActionResult> GetMapping()
        {
            try
            {
                var mapping = await _elasticsearchService.GetIndexMappingAsync();
                return Json(mapping);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("test-simple-search")]
        public async Task<IActionResult> TestSimpleSearch(string query = "asp")
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                
                _logger.LogInformation("Testing simple search with query: '{Query}' for user: '{UserId}'", query, userId);
                
                var result = await _elasticsearchService.SearchTasksAsync(query, userId, 1, 20);
                
                return Json(new 
                { 
                    query = query,
                    userId = userId,
                    totalResults = result.Total,
                    resultCount = result.Documents.Count,
                    searchTime = result.TookMilliseconds,
                    documents = result.Documents.Take(3).Select(d => new 
                    {
                        d.Id,
                        d.Title,
                        d.Description,
                        d.UserId,
                        d.IsDeleted
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test simple search");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("simple-test-search")]
        public async Task<IActionResult> SimpleTestSearch(string query = "asp")
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var result = await _elasticsearchService.SimpleTestSearchAsync(query, userId);
                
                return Json(new
                {
                    query = query,
                    userId = userId,
                    totalResults = result.Total,
                    resultCount = result.Documents.Count,
                    searchTime = result.TookMilliseconds,
                    documents = result.Documents.Take(3).Select(d => new 
                    {
                        d.Id,
                        d.Title,
                        d.Description,
                        d.UserId,
                        d.IsDeleted
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("test-match-all")]
        public async Task<IActionResult> TestMatchAll()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                
                // Test with empty query (should match all user's tasks)
                var result = await _elasticsearchService.SearchTasksAsync("", userId, 1, 20);
                
                return Json(new 
                { 
                    userId = userId,
                    totalResults = result.Total,
                    resultCount = result.Documents.Count,
                    message = "Empty query test (should return all user tasks)",
                    documents = result.Documents.Take(3).Select(d => new 
                    {
                        d.Id,
                        d.Title,
                        d.UserId,
                        d.IsDeleted
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost("recreate-index")]
        public async Task<IActionResult> RecreateIndex()
        {
            try
            {
                // Delete existing index
                await _elasticsearchService.DeleteIndexAsync();
                
                // Create new index
                var created = await _elasticsearchService.CreateIndexAsync();
                
                return Json(new { success = created, message = created ? "Index recreated successfully" : "Failed to create index" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}