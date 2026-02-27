namespace BulentOtoElektrik.Core.Interfaces;

public interface IExcelExportService
{
    Task ExportCustomerCardAsync(int customerId, int vehicleId, string filePath, CancellationToken ct = default);
    Task ExportReportAsync(DateTime startDate, DateTime endDate, string filePath, CancellationToken ct = default);
    Task AutoExportCustomerCardsAsync(int customerId, CancellationToken ct = default);
    Task AutoExportReportsAsync(DateTime date, CancellationToken ct = default);
    Task AutoExportAllAsync(CancellationToken ct = default);
    string GetExportFolder();
    void SetExportFolder(string path);
}
