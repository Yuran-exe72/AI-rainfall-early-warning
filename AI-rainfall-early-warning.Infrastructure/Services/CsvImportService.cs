using AI_rainfall_early_warning.Application.DTOs;
using AI_rainfall_early_warning.Application.Services;
using AI_rainfall_early_warning.Domain.Constants;
using AI_rainfall_early_warning.Domain.Entities;
using AI_rainfall_early_warning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AI_rainfall_early_warning.Infrastructure.Services;

public class CsvImportService : ICsvImportService
{
    private readonly ApplicationDbContext _db;

    public CsvImportService(ApplicationDbContext db) => _db = db;

    public async Task<ImportResultDto> ImportAsync(int regionId, string source, string fileName, Stream csvStream)
    {
        // Cabeçalho esperado por fonte
        var expectedHeader = source switch
        {
            DomainConstants.Sources.Chirps => "date,rainfall_mm",
            DomainConstants.Sources.Era5Land => "date,temperature_2m_c",
            DomainConstants.Sources.Smap => "date,soil_moisture_surface",
            _ => null
        };
        if (expectedHeader is null)
            return new ImportResultDto(false, 0, 0, 0, $"Fonte desconhecida: {source}");

        if (!await _db.Regions.AnyAsync(r => r.Id == regionId))
            return new ImportResultDto(false, 0, 0, 0, $"Região {regionId} não existe.");

        // Intervalos físicos válidos — rejeita dados impossíveis
        (double Min, double Max) range = source switch
        {
            DomainConstants.Sources.Chirps => (0, 250),
            DomainConstants.Sources.Era5Land => (-5, 55),
            _ => (0, 1)
        };

        using var reader = new StreamReader(csvStream);
        var header = reader.ReadLine()?.Trim();
        if (header is null || !header.Equals(expectedHeader, StringComparison.OrdinalIgnoreCase))
            return new ImportResultDto(false, 0, 0, 0, $"Cabeçalho inválido. Esperado '{expectedHeader}', recebido '{header}'.");

        // 1) Parsing + validação
        var validRows = new List<(DateTime Date, double Value)>();
        var errors = new List<string>();
        int rejected = 0, lineNumber = 1;

        while (await reader.ReadLineAsync() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            DateTime date = default;
            double value = default;
            var ok = parts.Length == 2
                && DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                && double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && value >= range.Min && value <= range.Max;
            if (!ok)
            {
                rejected++;
                if (errors.Count < 5) errors.Add($"linha {lineNumber}: '{line}'");
                continue;
            }
            validRows.Add((date, value));
        }

        // 2) Registo de auditoria (precisa do Id antes das observações)
        var import = new DataImport
        {
            FileName = fileName,
            Source = source,
            ImportedAt = DateTime.UtcNow,
            RowsImported = validRows.Count,
            RowsRejected = rejected,
            ErrorsSummary = errors.Count > 0 ? string.Join("; ", errors) : null
        };
        _db.DataImports.Add(import);
        await _db.SaveChangesAsync();

        // 3) Upsert: insere novos, actualiza existentes (chave única Região+Data+Fonte)
        var existing = await _db.ClimateObservations
            .Where(o => o.RegionId == regionId && o.Source == source)
            .ToDictionaryAsync(o => o.Date, o => o);

        int inserted = 0, updated = 0;

        foreach (var (date, value) in validRows)
        {
            if (existing.TryGetValue(date, out var obs))
            {
                SetValue(obs, source, value);
                updated++;
            }
            else
            {
                var newObs = new ClimateObservation
                {
                    RegionId = regionId,
                    Date = date,
                    Source = source,
                    ImportId = import.Id
                };
                SetValue(newObs, source, value);
                _db.ClimateObservations.Add(newObs);
                existing[date] = newObs;
                inserted++;
            }
        }
        await _db.SaveChangesAsync();

        return new ImportResultDto(true, inserted, updated, rejected);
    }

    private static void SetValue(ClimateObservation obs, string source, double value)
    {
        switch (source)
        {
            case DomainConstants.Sources.Chirps: obs.RainfallMm = value; break;
            case DomainConstants.Sources.Era5Land: obs.TemperatureC = value; break;
            default: obs.SoilMoisture = value; break;
        }
    }
}