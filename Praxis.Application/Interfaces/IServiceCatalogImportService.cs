namespace Praxis.Application.Interfaces;

public interface IServiceCatalogImportService
{
    Task ImportAsync(string filePath);
}