using System;
using NejPortalBackend.Application.Common.Interfaces;
using System.Text;

namespace NejPortalBackend.Infrastructure.Services;

public class CsvExportService : ICsvExportService
{
    public string ExportToCsv<T>(IEnumerable<T> data)
    {
        if (data == null || !data.Any())
            return string.Empty;

        var properties = typeof(T).GetProperties();
        var csv = new StringBuilder();

        // Header row
        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Data rows
        foreach (var item in data)
        {
            var values = properties.Select(p => p.GetValue(item, null)?.ToString() ?? string.Empty);
            csv.AppendLine(string.Join(",", values.Select(v => $"\"{v}\""))); // Add quotes to handle commas in data
        }

        return csv.ToString();
    }
}

