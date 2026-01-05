using Microsoft.AspNetCore.Components.Forms;

namespace GachaSimulator.Services;

public class FileUploadService
{
    private readonly IWebHostEnvironment _environment;

    public FileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadFileAsync(IBrowserFile file)
    {
        if (file == null) return "";
        // Đường dẫn vật lý: wwwroot/images/uploads
        var uploadPath = Path.Combine(_environment.WebRootPath, "images", "uploads");
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var extension = Path.GetExtension(file.Name);
        var newFileName = $"{Path.GetFileNameWithoutExtension(file.Name)}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, newFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
        }

        return $"/images/uploads/{newFileName}";
    }
}