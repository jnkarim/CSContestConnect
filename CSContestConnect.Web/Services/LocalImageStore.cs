// CSContestConnect.Web/Services/LocalImageStore.cs
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CSContestConnect.Web.Services
{
    public class LocalImageStore : IImageStore
    {
        private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

        public async Task<string> SaveProfileImageAsync(IFormFile file, string webRootPath, string? existingRelativePath = null)
        {
            if (file == null || file.Length == 0) return existingRelativePath ?? string.Empty;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Allowed.Contains(ext)) throw new InvalidOperationException("Only JPG/PNG/WEBP/GIF allowed.");

            var folder = Path.Combine(webRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            if (!string.IsNullOrWhiteSpace(existingRelativePath))
            {
                var oldPath = Path.Combine(webRootPath, existingRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(absPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var rel = $"/uploads/profiles/{fileName}";
            return rel;
        }

        public async Task DeleteProfileImageAsync(string? imagePath, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            var fullPath = Path.Combine(webRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }
    }
}