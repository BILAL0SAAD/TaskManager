// Configuration/ElasticsearchSettings.cs
namespace TaskManager.Web.Configuration
{
    public class ElasticsearchSettings
    {
        public string Uri { get; set; } = "http://localhost:9200";
        public string DefaultIndex { get; set; } = "taskmanager";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableLogging { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 60;
    }
}