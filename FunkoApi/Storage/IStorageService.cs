namespace FunkoApi.Storage;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
    void DeleteFile(string fileName);
}

