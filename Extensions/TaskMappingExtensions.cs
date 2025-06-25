// Extensions/TaskMappingExtensions.cs
using TaskManager.Web.Models;
using TaskManager.Web.Models.Elasticsearch;

namespace TaskManager.Web.Extensions
{
    public static class TaskMappingExtensions
    {
        public static TaskDocument ToElasticsearchDocument(this TaskItem task)
        {
            return new TaskDocument
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                UserId = task.UserId,
                IsOverdue = task.IsOverdue,
                IsDeleted = task.IsDeleted,
                Project = task.Project?.ToElasticsearchDocument(),
                Comments = task.Comments?.Select(c => c.ToElasticsearchDocument()).ToList() ?? new(),
                Tags = ExtractTags(task)
            };
        }

        public static ProjectDocument ToElasticsearchDocument(this Project project)
        {
            return new ProjectDocument
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Color = project.Color
            };
        }

        public static TaskCommentDocument ToElasticsearchDocument(this TaskComment comment)
        {
            return new TaskCommentDocument
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId
            };
        }

        private static List<string> ExtractTags(TaskItem task)
        {
            var tags = new List<string>();
            
            // Add priority as tag
            tags.Add($"priority:{task.Priority.ToString().ToLower()}");
            
            // Add status as tag
            tags.Add($"status:{task.Status.ToString().ToLower()}");
            
            // Add project name if exists
            if (task.Project != null)
                tags.Add($"project:{task.Project.Name.ToLower()}");
            
            // Extract tags from title/description (basic implementation)
            var words = $"{task.Title} {task.Description}".Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words.Where(w => w.StartsWith('#')))
            {
                tags.Add(word.TrimStart('#').ToLower());
            }
            
            return tags;
        }
    }
}