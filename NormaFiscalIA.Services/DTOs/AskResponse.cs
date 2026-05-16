namespace NormaFiscalIA.Services.DTOs
{
    public class AskResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<SourceInfo> Sources { get; set; } = new();
        public string ModeUsed { get; set; } = string.Empty;
    }

    public class SourceInfo
    {
        public string Document { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
    }
}