namespace BulentOtoElektrik.Core.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(CancellationToken ct = default);
    Task<List<string>> GetBackupsAsync(CancellationToken ct = default);
    Task CleanupOldBackupsAsync(int keepCount = 30, CancellationToken ct = default);
    string GetBackupFolder();
    void SetBackupFolder(string path);
}
