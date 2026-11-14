namespace AI_rainfall_early_warning.Application.DTOs;

public record ImportResultDto(bool Success, int RowsImported, int RowsUpdated, int RowsRejected, string? Error = null);