namespace AI_rainfall_early_warning.Domain.Constants;

public static class DomainConstants
{
    public static class Sources
    {
        public const string Chirps = "CHIRPS";
        public const string Era5Land = "ERA5-Land";
        public const string Smap = "SMAP";
    }

    public static class RiskLevels
    {
        public const string Normal = "Normal";
        public const string Alerta = "Alerta";
        public const string SecaSevera = "Seca Severa";
        public const string SecaExtrema = "Seca Extrema";
    }

    public static class AlertSeverity
    {
        public const string Info = "Informativo";
        public const string Aviso = "Aviso";
        public const string Critico = "Crítico";
    }

    public static class AlertStatus
    {
        public const string Novo = "Novo";
        public const string Reconhecido = "Reconhecido";
        public const string Resolvido = "Resolvido";
    }
}