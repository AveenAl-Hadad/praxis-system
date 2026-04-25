namespace Praxis.Application.Interfaces;

public interface IIcdImportService
{
    Task ImportAsync(string xmlPath);
}