using System.Globalization;
using CsvHelper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Application.Common.Vms;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IIdentityService _identityService;

    public FileService(IWebHostEnvironment hostingEnvironment,IIdentityService identityService)
    {
        _hostingEnvironment = hostingEnvironment;
        _identityService = identityService;
    }



    public async Task<FileInformationDto> Create(IFormFile file, string mainFolderName, string userName, int operationId)
    {
        // Generate a unique file name
        
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

        // Define the path to save the file
        var folderPath = Path.Combine(_hostingEnvironment.WebRootPath, mainFolderName, userName, $"{operationId}");
        var filePath = Path.Combine(folderPath, fileName);

        // Check if the directory exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Copy the file to the server
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create a new FileInformation object
        var fileInfo = new FileInformationDto
        {
            NomDocument = file.FileName,
            CheminFichier = filePath,
            TailleFichier = file.Length,
            TypeFichier = file.ContentType
        };

        return fileInfo;
    }

    public async Task<ExportOperationsVm> GenerateExportFile(IEnumerable<OperationDto> operations, bool isCsvExport,DateTime date)
    {
        if (operations == null || !operations.Any())
        {
            throw new ArgumentNullException(nameof(operations), "Operations list cannot be null or empty.");
        }
        ExportOperationsVm exportVm = new ExportOperationsVm();
        string fileName = $"export_"+ date.ToShortDateString();

        try
        {
            if (isCsvExport)
            {
                exportVm.FileContent = await GenerateCsvFile(operations);
                exportVm.FileName = $"{fileName}.csv";
                exportVm.ContentType = "text/csv";
            }
            else
            {
                exportVm.FileContent = await GeneratePdfFile(operations);
                exportVm.FileName = $"{fileName}.pdf";
                exportVm.ContentType = "application/pdf";
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error generating export file: {ex.Message ?? ex.GetType().Name}");
            throw;
        }

        return exportVm;
    }

    private static async Task<byte[]> GenerateCsvFile(IEnumerable<OperationDto> operations)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

        await csvWriter.WriteRecordsAsync(operations);

        ms.Position = 0;
        return ms.ToArray();
    }

    private  async Task<byte[]> GeneratePdfFile(IEnumerable<OperationDto> operations)
    {
        return await Task.Run(async () =>
        {
            using var ms = new MemoryStream();
            using var doc = new Document();
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var font = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var paragraph = new Paragraph("Operations Report", font);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);

            var table = new PdfPTable(5)
            {
                WidthPercentage = 100
            };

            PdfPCell headerCell = new(new Phrase("Id"))
            {
                BackgroundColor = BaseColor.LightGray
            };
            table.AddCell(headerCell);

            headerCell = new PdfPCell(new Phrase("Type Operation"));
            table.AddCell(headerCell);

            headerCell = new PdfPCell(new Phrase("Etat Operation"));
            table.AddCell(headerCell);

            headerCell = new PdfPCell(new Phrase("Created"));
            table.AddCell(headerCell);

            headerCell = new PdfPCell(new Phrase("Code Dossier"));
            headerCell = new PdfPCell(new Phrase("Code Client"));
            table.AddCell(headerCell);

            foreach (var operation in operations)
            {
                var codeClient = await _identityService.GetCodeClientAsync(operation.UserId ?? string.Empty);
                table.AddCell(operation.Id.ToString());
                table.AddCell(operation.TypeOperation.ToString());
                table.AddCell(operation.EtatOperation.ToString());
                table.AddCell(operation.Created.ToString());
                table.AddCell(operation.CodeDossier);
                table.AddCell(codeClient);
            }

            doc.Add(table);
            doc.Close();

            ms.Position = 0;
            return ms.ToArray();
        });
    }


}