// Services/IFileService.cs
namespace TaskManager.Web.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> DeleteFileAsync(string filePath);
        Task<(Stream stream, string contentType)> GetFileAsync(string filePath);
    }
}

