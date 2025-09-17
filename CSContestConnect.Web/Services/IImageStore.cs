// CSContestConnect.Web/Services/IImageStore.cs
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CSContestConnect.Web.Services
{
    public interface IImageStore
    {
        Task<string> SaveProfileImageAsync(IFormFile file, string webRootPath, string? existingImagePath);
        // Optionally, you can add a method for deletion if needed
        Task DeleteProfileImageAsync(string? imagePath, string webRootPath);
    }
}