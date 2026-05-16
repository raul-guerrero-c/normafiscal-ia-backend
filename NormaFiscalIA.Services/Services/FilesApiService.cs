using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NormaFiscalIA.Services.Services
{
    public class FilesApiService
    {
        private readonly AnthropicClient _client;
        private readonly ILogger<FilesApiService> _logger;
        private readonly string _documentsPath;
        private List<string> _uploadedFileIds = new();

        public FilesApiService(IConfiguration config, ILogger<FilesApiService> logger)
        {
            var apiKey = config["APIs:Anthropic:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("API Key no configurada");

            _client = new AnthropicClient
            {
                ApiKey = apiKey,
                Timeout = TimeSpan.FromSeconds(60)
            };

            _logger = logger;
            //_documentsPath = Path.Combine(AppContext.BaseDirectory, "Documents", "Legal");
            // A ESTO (ruta relativa correcta):
            _documentsPath = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",  // Subir 3 niveles: bin/Debug/net10.0 → NormaFiscalIA.API
                "Documents",
                "Legal"
            );
        }

        /// <summary>
        /// Subir un archivo usando Files API (sintaxis oficial)
        /// </summary>
        public async Task<string> UploadFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"No encontrado: {filePath}");

                var fileName = Path.GetFileName(filePath);
                _logger.LogInformation($"Subiendo: {fileName}");

                // SINTAXIS CORRECTA: client.Beta.Files.Upload
                using (var fileStream = File.OpenRead(filePath))
                {
                    var uploaded = await _client.Beta.Files.Upload(
                        new FileUploadParams
                        {
                            File = fileStream
                        });

                    _logger.LogInformation($"✓ {fileName} subido (ID: {uploaded.ID})");
                    _uploadedFileIds.Add(uploaded.ID);

                    return uploaded.ID;
                }
            }
            catch (AnthropicApiException ex)
            {
                _logger.LogError($"Error API: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error subiendo archivo: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subir todos los documentos de la carpeta
        /// </summary>
        public async Task<List<string>> UploadAllDocumentsAsync()
        {
            try
            {
                _uploadedFileIds.Clear();

                if (!Directory.Exists(_documentsPath))
                {
                    _logger.LogWarning($"Carpeta no existe: {_documentsPath}");
                    return _uploadedFileIds;
                }

                var archivos = Directory.GetFiles(_documentsPath, "*.pdf")
                    .OrderBy(x => x)
                    .ToList();

                _logger.LogInformation($"Encontrados {archivos.Count} documentos para subir");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        await UploadFileAsync(archivo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error en {Path.GetFileName(archivo)}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"✓ Subida completada: {_uploadedFileIds.Count}/{archivos.Count}");
                return _uploadedFileIds;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error general: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtener IDs de archivos subidos
        /// </summary>
        public List<string> GetUploadedFileIds()
        {
            return new List<string>(_uploadedFileIds);
        }

        /// <summary>
        /// Listar archivos usando FileListPageResponse (sintaxis oficial)
        /// </summary>
        public async Task<List<FileListItem>> ListFilesAsync()
        {
            try
            {
                _logger.LogInformation("Listando archivos...");

                // SINTAXIS CORRECTA: client.Beta.Files.List retorna FileListPageResponse
                var parameters = new FileListParams();
                var page = await _client.Beta.Files.List(parameters);

                var archivos = new List<FileListItem>();

                // SINTAXIS CORRECTA: Usar Paginate() para iterar todos los resultados
                await foreach (var fileMetadata in page.Paginate())
                {
                    archivos.Add(new FileListItem
                    {
                        Id = fileMetadata.ID,
                        Filename = fileMetadata.Filename,
                        MimeType = fileMetadata.MimeType,
                        SizeBytes = fileMetadata.SizeBytes,
                        CreatedAt = fileMetadata.CreatedAt,
                        Downloadable = fileMetadata.Downloadable ?? false
                    });
                }

                if(archivos.Count > 0)
                {
                    _logger.LogInformation($"✓ Total archivos encontrados: {archivos.Count}");
                    // Log info de paginación
                    _logger.LogInformation($"  FirstID: {page.Items.First().ID}, LastID: {page.Items.Last().ID}, HasMore: {page.HasNext()}");
                }
                else
                {
                    _logger.LogInformation("✓ No se encontraron archivos.");
                }

                return archivos;
            }
            catch (AnthropicApiException ex)
            {
                _logger.LogError($"Error API listando: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error listando archivos: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Listar archivos con paginación manual (alternativa)
        /// </summary>
        public async Task<(List<FileListItem> Items, bool HasMore, string FirstId, string LastId)>
            ListFilesWithPaginationAsync(int limit = 20, string afterId = null)
        {
            try
            {
                _logger.LogInformation($"Listando archivos (limit: {limit}, after: {afterId})");

                // SINTAXIS CORRECTA: FileListParams con parámetros de paginación
                var parameters = new FileListParams
                {
                    Limit = limit,
                    AfterID = afterId
                };

                var page = await _client.Beta.Files.List(parameters);

                var archivos = page.Items.Select(f => new FileListItem
                {
                    Id = f.ID,
                    Filename = f.Filename,
                    MimeType = f.MimeType,
                    SizeBytes = f.SizeBytes,
                    CreatedAt = f.CreatedAt,
                    Downloadable = f.Downloadable ?? false
                }).ToList();

                _logger.LogInformation($"✓ Página obtenida: {archivos.Count} archivos");

                return (archivos, page.HasNext(), page.Items.First().ID, page.Items.Last().ID);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en paginación: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Eliminar archivo (sintaxis oficial)
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileId))
                    throw new ArgumentException("fileId no puede estar vacío");

                // SINTAXIS CORRECTA: client.Beta.Files.Delete
                await _client.Beta.Files.Delete(fileId);

                _logger.LogInformation($"✓ Eliminado: {fileId}");
                _uploadedFileIds.Remove(fileId);

                return true;
            }
            catch (AnthropicApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogError($"Archivo no encontrado: {fileId}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error eliminando: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtener información de un archivo específico
        /// </summary>
        public async Task<FileListItem?> GetFileInfoAsync(string fileId)
        {
            try
            {
                var archivos = await ListFilesAsync();
                return archivos.FirstOrDefault(f => f.Id == fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo info: {ex.Message}");
                return null;
            }
        }
    }

    // MODELOS para respuesta
    public class FileListItem
    {
        public string Id { get; set; }
        public string Filename { get; set; }
        public string MimeType { get; set; }
        public long SizeBytes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool Downloadable { get; set; }
    }
}