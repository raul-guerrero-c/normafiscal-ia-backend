namespace NormaFiscalIA.Services.Interfaces
{
    public interface IOpenAIService
    {
        Task<string> ConsultarAsync(string consulta, string systemPrompt, int maxTokens = 2000);
        Task<bool> VerificarConexionAsync();
    }
}
