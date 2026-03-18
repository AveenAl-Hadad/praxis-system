namespace Praxis.Infrastructure.Services;

public interface IBackupService
{
    Task CreateBackupAsync(string backupFilePath);
    Task RestoreBackupAsync(string backupFilePath);
}