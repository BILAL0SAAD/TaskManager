// ViewModels/Search/SearchAnalyticsViewModel.cs
using TaskManager.Web.Models.Elasticsearch;

namespace TaskManager.Web.ViewModels.Search
{
    public class SearchAnalyticsViewModel
    {
        public Dictionary<string, long> StatusDistribution { get; set; } = new();
        public Dictionary<string, long> PriorityDistribution { get; set; } = new();
        public List<TaskDocument> OverdueTasks { get; set; } = new();
        public int TotalOverdue { get; set; }
        public Dictionary<string, long> ProjectDistribution { get; set; } = new();
        public List<SearchTrendItem> SearchTrends { get; set; } = new();
        
        // Performance metrics
        public double AverageSearchTime { get; set; }
        public long TotalSearches { get; set; }
        public Dictionary<string, int> PopularQueries { get; set; } = new();
    }
    
    public class SearchTrendItem
    {
        public string Query { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastSearched { get; set; }
    }
}

// ViewModels/Search/AdvancedSearchViewModel.cs
namespace TaskManager.Web.ViewModels.Search
{
    public class AdvancedSearchViewModel
    {
        // Basic search
        public string Query { get; set; } = string.Empty;
        public string SearchType { get; set; } = "all"; // all, title, description, comments
        
        // Advanced filters
        public List<string>? Statuses { get; set; }
        public List<string>? Priorities { get; set; }
        public List<int>? ProjectIds { get; set; }
        public List<string>? Tags { get; set; }
        
        // Date ranges
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? DueFrom { get; set; }
        public DateTime? DueTo { get; set; }
        public DateTime? CompletedFrom { get; set; }
        public DateTime? CompletedTo { get; set; }
        
        // Content filters
        public bool HasComments { get; set; }
        public bool HasAttachments { get; set; }
        public bool IsOverdue { get; set; }
        public int? MinComments { get; set; }
        
        // Sorting
        public string SortBy { get; set; } = "relevance"; // relevance, created, due, title
        public string SortOrder { get; set; } = "desc"; // asc, desc
        
        // Results
        public List<TaskDocument> Results { get; set; } = new();
        public long TotalResults { get; set; }
        public long SearchTime { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Export options
        public bool EnableExport { get; set; } = true;
        public string ExportFormat { get; set; } = "json"; // json, csv, excel
    }
}