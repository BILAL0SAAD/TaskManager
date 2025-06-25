// ViewModels/Search/SearchViewModel.cs
using TaskManager.Web.Models.Elasticsearch;

namespace TaskManager.Web.ViewModels.Search
{
    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Results
        public List<TaskDocument> Results { get; set; } = new();
        public long TotalResults { get; set; }
        public long SearchTime { get; set; }
        public bool HasResults => Results.Any();
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
        
        // Filters
        public List<string>? SelectedStatuses { get; set; }
        public List<string>? SelectedPriorities { get; set; }
        public int? ProjectId { get; set; }
        public bool ShowOverdueOnly { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        
        // Filter Options
        public List<string> StatusOptions { get; set; } = new()
        {
            "Todo", "InProgress", "Review", "Done", "Cancelled"
        };
        
        public List<string> PriorityOptions { get; set; } = new()
        {
            "Low", "Medium", "High", "Critical"
        };
        
        // Pagination
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public int PreviousPage => Page - 1;
        public int NextPage => Page + 1;
        
        // Search suggestions
        public List<string> RecentSearches { get; set; } = new();
        public List<string> PopularSearches { get; set; } = new();
    }
}