using System;
namespace NejPortalBackend.Application.Common.Interfaces;

public interface ICsvExportService
{
    string ExportToCsv<T>(IEnumerable<T> data);
}

