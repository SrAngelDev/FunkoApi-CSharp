using Path = System.IO.Path;

namespace FunkoApi.Storage;


public class LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger) 
    : IStorageService
{
    // Directorio donde se guardan las imágenes (dentro de wwwroot)
    private readonly string _uploadsFolder = Path.Combine(environment.WebRootPath, "images");

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        // Validar extensión 
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".png" && extension != ".jpeg" && extension != ".webp")
        {
            throw new ArgumentException("Formato de imagen no soportado");
        }

        // Generar nombre único para evitar colisiones
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsFolder, fileName);

        // Crear directorio si no existe
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }

        // Guardar fichero
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        logger.LogInformation($"Imagen guardada en: {filePath}");

        return fileName; // Devolvemos el nombre para guardarlo en la BD
    }

    public void DeleteFile(string fileName)
    {
        var filePath = Path.Combine(_uploadsFolder, fileName);
        
        // No borramos la imagen por defecto ni urls externas
        if (File.Exists(filePath) && !fileName.StartsWith("https"))
        {
            File.Delete(filePath);
            logger.LogInformation($"Imagen borrada: {filePath}");
        }
    }
}