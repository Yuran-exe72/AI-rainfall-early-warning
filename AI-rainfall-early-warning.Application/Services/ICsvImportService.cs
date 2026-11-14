using AI_rainfall_early_warning.Application.DTOs;

namespace AI_rainfall_early_warning.Application.Services;

public interface ICsvImportService
{
    Task<ImportResultDto> ImportAsync(int regionId, string source, string fileName, Stream csvStream);
}