using BulentOtoElektrik.Core.Interfaces;

namespace BulentOtoElektrik.Infrastructure.Services;

public class BackupService : IBackupService
{
    private string _backupFolder;
    private readonly string _dbPath;

    public BackupService()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bulentoto.db");
        _backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
    }

    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_backupFolder);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupPath = Path.Combine(_backupFolder, $"bulentoto_backup_{timestamp}.db");

        if (File.Exists(_dbPath))
        {
            await Task.Run(() => File.Copy(_dbPath, backupPath, overwrite: true), ct);
        }

        await CleanupOldBackupsAsync(30, ct);
        return backupPath;
    }

    public Task<List<string>> GetBackupsAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_backupFolder);
        var backups = Directory.GetFiles(_backupFolder, "bulentoto_backup_*.db")
            .OrderByDescending(f => f)
            .ToList();
        return Task.FromResult(backups);
    }

    public Task CleanupOldBackupsAsync(int keepCount = 30, CancellationToken ct = default)
    {
        var backups = Directory.GetFiles(_backupFolder, "bulentoto_backup_*.db")
            .OrderByDescending(f => f)
            .ToList();
        foreach (var old in backups.Skip(keepCount))
        {
            try { File.Delete(old); } catch { }
        }
        return Task.CompletedTask;
    }

    public string GetBackupFolder() => _backupFolder;

    public void SetBackupFolder(string path)
    {
        _backupFolder = path;
        Directory.CreateDirectory(_backupFolder);
    }
}
