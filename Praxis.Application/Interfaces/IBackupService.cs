namespace Praxis.Application.Interfaces
{ 

public interface IBackupService
{
    Task CreateBackupAsync(string backupFilePath);
    Task RestoreBackupAsync(string backupFilePath);
}
}