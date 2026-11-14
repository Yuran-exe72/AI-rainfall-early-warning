using System.Globalization;
using System.Text;

// ============================================================
// Gerador de dados sintéticos — simula o formato de:
//   CHIRPS    -> precipitação diária (mm)
//   ERA5-Land -> temperatura média a 2m (°C)
//   SMAP      -> humidade do solo de superfície (0-1)
// Regiões: Mabote e Govuro (Inhambane, Moçambique)
// Período: 2023-01-01 a 2026-11-13 | Seed fixa = reproduzível
// ============================================================

var rand = new Random(42);
var startDate = new DateTime(2023, 1, 1);
var endDate = new DateTime(2026, 11, 13);
var culture = CultureInfo.InvariantCulture;

var regions = new (string Folder, double RainFactor)[]
{
    ("mabote", 0.85),   // mais seco
    ("govuro", 1.00),
};

// Temperatura média mensal (°C) — sul de Moçambique
double[] tempMediaMes = { 26.5, 26.0, 25.5, 23.5, 21.0, 19.0, 18.5, 20.0, 22.5, 24.5, 25.5, 26.5 };

string dir = Path.Combine("data", "raw");
Directory.CreateDirectory(dir);

foreach (var region in regions)
{
    var sbChirps = new StringBuilder("date,rainfall_mm\n");
    var sbEra5 = new StringBuilder("date,temperature_2m_c\n");
    var sbSmap = new StringBuilder("date,soil_moisture_surface\n");

    double soilState = 0.25;

    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        int month = date.Month;
        bool epocaChuvosa = month >= 11 || month <= 3;   // Nov-Mar
        bool pico = month == 1 || month == 2;            // Jan-Fev

        // Seca simulada (estilo El Niño): Out 2025 - Fev 2026
        bool secaSimulada = date >= new DateTime(2025, 10, 1) && date <= new DateTime(2026, 2, 28);

        // ---- CHIRPS: precipitação ----
        double probChuva = pico ? 0.45 : epocaChuvosa ? 0.30 : 0.04;
        double mediaMolhada = pico ? 22.0 : epocaChuvosa ? 14.0 : 5.0;
        if (secaSimulada) { probChuva *= 0.35; mediaMolhada *= 0.50; }

        double mm = 0.0;
        if (rand.NextDouble() < probChuva)
        {
            mm = -mediaMolhada * Math.Log(1.0 - rand.NextDouble()); // exponencial
            mm = Math.Min(mm, 120.0);
        }
        mm = Math.Round(mm * region.RainFactor, 1);
        sbChirps.AppendLine($"{date:yyyy-MM-dd},{mm.ToString("0.0", culture)}");

        // ---- SMAP: humidade do solo ----
        soilState += mm / 60.0;      // recarga pela chuva
        soilState *= 0.972;          // evapotranspiração
        if (secaSimulada) soilState *= 0.995;
        soilState = Math.Clamp(soilState, 0.02, 0.55);
        sbSmap.AppendLine($"{date:yyyy-MM-dd},{soilState.ToString("0.000", culture)}");

        // ---- ERA5-Land: temperatura ----
        double temp = tempMediaMes[month - 1] + (rand.NextDouble() - 0.5) * 4.0;
        if (secaSimulada && !epocaChuvosa) temp += 2.0;   // ondas de calor
        sbEra5.AppendLine($"{date:yyyy-MM-dd},{temp.ToString("0.0", culture)}");
    }

    File.WriteAllText(Path.Combine(dir, $"{region.Folder}_chirps.csv"), sbChirps.ToString());
    File.WriteAllText(Path.Combine(dir, $"{region.Folder}_era5_land.csv"), sbEra5.ToString());
    File.WriteAllText(Path.Combine(dir, $"{region.Folder}_smap.csv"), sbSmap.ToString());
    Console.WriteLine($"OK: {region.Folder} -> 3 ficheiros gerados");
}

Console.WriteLine($"\nConcluído! {(int)((endDate - startDate).TotalDays) + 1} dias por ficheiro.");
Console.WriteLine($"Ficheiros em: {Path.GetFullPath(dir)}");