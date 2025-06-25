// Models/Elasticsearch/TaskDocument.cs
using Nest;

namespace TaskManager.Web.Models.Elasticsearch
{
    [ElasticsearchType(IdProperty = nameof(Id))]
    public class TaskDocument
    {
        public int Id { get; set; }
        
        [Text(Analyzer = "standard")]
        public string Title { get; set; } = string.Empty;
        
        [Text(Analyzer = "standard")]
        public string? Description { get; set; }
        
        [Keyword]
        public string Priority { get; set; } = string.Empty;
        
        [Keyword] 
        public string Status { get; set; } = string.Empty;
        
        [Date]
        public DateTime? DueDate { get; set; }
        
        [Date]
        public DateTime CreatedAt { get; set; }
        
        [Date]
        public DateTime? CompletedAt { get; set; }
        
        [Keyword]
        public string UserId { get; set; } = string.Empty;
        
        [Nested]
        public ProjectDocument? Project { get; set; }
        
        [Nested]
        public List<TaskCommentDocument> Comments { get; set; } = new();
        
        [Text(Analyzer = "standard")]
        public string SearchContent { get; set; } = string.Empty;
        
        [Keyword]
        public List<string> Tags { get; set; } = new();
        
        [Boolean]
        public bool IsOverdue { get; set; }
        
        [Boolean]
        public bool IsDeleted { get; set; }

        // COMMENTED OUT:
        // [Completion]
        // public CompletionField Suggest { get; set; } = new();
    }

    public class ProjectDocument
    {
        public int Id { get; set; }
        
        [Text(Analyzer = "standard")]
        public string Name { get; set; } = string.Empty;
        
        [Text(Analyzer = "standard")]  
        public string? Description { get; set; }
        
        [Keyword]
        public string Color { get; set; } = string.Empty;
    }

    public class TaskCommentDocument
    {
        public int Id { get; set; }
        
        [Text(Analyzer = "standard")]
        public string Content { get; set; } = string.Empty;
        
        [Date]
        public DateTime CreatedAt { get; set; }
        
        [Keyword]
        public string UserId { get; set; } = string.Empty;
    }
}