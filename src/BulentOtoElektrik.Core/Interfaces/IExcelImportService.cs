using BulentOtoElektrik.Core.DTOs;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IExcelImportService
{
    Task<ExcelFileParseResultDto> ParseFileAsync(string filePath, CancellationToken ct = default);

    Task<ExcelImportResultDto> ImportFilesAsync(
        IReadOnlyList<string> filePaths,
        string duplicateAction,
        Action<int, int>? progressCallback = null,
        CancellationToken ct = default);

    Task<ExcelImportResultDto> ImportFolderAsync(
        string folderPath,
        string duplicateAction,
        Action<int, int>? progressCallback = null,
        CancellationToken ct = default);
}
