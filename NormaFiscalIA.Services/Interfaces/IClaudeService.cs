namespace NormaFiscalIA.Services.Interfaces
{
    public interface IClaudeService
    {
        Task<string> ConsultarAsync(string consulta, string systemPrompt, int maxTokens = 1500);
        Task<bool> VerificarConexionAsync();
    }
}
