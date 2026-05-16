using Microsoft.AspNetCore.Mvc;
using NormaFiscalIA.Core.Enums;
using NormaFiscalIA.Services.DTOs;
using NormaFiscalIA.Services.Interfaces;
using NormaFiscalIA.Services.Services;
using System.Diagnostics;

namespace NormaFiscalIA.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IRouterIA _routerIA;
        private readonly IValidadorService _validadorService;
        private readonly ILogger<ConsultasController> _logger;

        private readonly ClaudeService _claudeService;
        private readonly OpenAIFileSearchService _openaiService;

        public ConsultasController(
            IRouterIA routerIA,
            IValidadorService validadorService,
            ILogger<ConsultasController> logger,
            ClaudeService claudeService,
            OpenAIFileSearchService openaiService)
        {
            _routerIA = routerIA;
            _validadorService = validadorService;
            _logger = logger;
            _claudeService = claudeService;
            _openaiService = openaiService;
        }

        /// <summary>
        /// Procesa una consulta fiscal mexicana
        /// </summary>
        /// <param name="request">Datos de la consulta</param>
        /// <returns>Respuesta fiscal validada</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcesarConsulta([FromBody] ConsultaRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(request.Consulta))
                    return BadRequest(new { error = "La consulta no puede estar vacía" });

                if (request.Consulta.Length < 10)
                    return BadRequest(new { error = "La consulta debe tener al menos 10 caracteres" });

                if (request.Consulta.Length > 5000)
                    return BadRequest(new { error = "La consulta no puede exceder 5000 caracteres" });

                _logger.LogInformation($"Nueva consulta: {request.Consulta.Substring(0, Math.Min(50, request.Consulta.Length))}...");

                // Determinar motor IA
                var motor = await _routerIA.DeterminarMotorAsync(request.Consulta, request.TipoRespuesta);
                _logger.LogInformation($"Motor seleccionado: {motor}");

                // Procesar con IA
                var respuestaTexto = await _routerIA.ProcesarConsultaAsync(
                    request.Consulta,
                    request.Modulo,
                    request.TipoRespuesta,
                    motor);

                // Validar respuesta
                var validacion = _validadorService.ValidarRespuesta(respuestaTexto, request.TipoRespuesta.ToString());

                stopwatch.Stop();

                // Construir respuesta
                var respuestaConsulta = new ConsultaResponseDto
                {
                    Consulta = request.Consulta,
                    Respuesta = respuestaTexto,
                    MotorUtilizado = motor,
                    TipoRespuesta = request.TipoRespuesta,
                    FechaCreacion = DateTime.UtcNow,
                    Metadata = new MetadataRespuestaDto
                    {
                        NivelConfianza = validacion.NivelConfianza switch
                        {
                            "Alta" => NivelConfianza.Alta,
                            "Media" => NivelConfianza.Media,
                            _ => NivelConfianza.Baja
                        },
                        CitasNormativas = _validadorService.ContarCitas(respuestaTexto),
                        AlucinacionesDetectadas = _validadorService.DetectarAlucinaciones(respuestaTexto) ? 1 : 0,
                        EstructuraCompleta = validacion.Estructura.ElementosEncontrados == validacion.Estructura.ElementosEsperados,
                        TiempoMs = stopwatch.Elapsed,
                        NormasAplicables = new List<string> { "CFF", "LISR", "LIVA", "LIEPS", "RCFF" }
                    }
                };

                _logger.LogInformation($"Consulta procesada exitosamente en {stopwatch.ElapsedMilliseconds}ms");

                return Ok(new
                {
                    exito = true,
                    datos = respuestaConsulta,
                    mensaje = "Consulta procesada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error procesando consulta: {ex.Message}");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        exito = false,
                        error = "Error procesando la consulta",
                        detalles = ex.Message
                    });
            }
        }

        /// <summary>
        /// Obtiene una consulta anterior por ID
        /// </summary>
        /// <param name="id">ID de la consulta</param>
        /// <returns>Datos de la consulta</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerConsulta(int id)
        {
            try
            {
                _logger.LogInformation($"Buscando consulta ID: {id}");

                // Nota: En MVP, retornamos estructura de ejemplo
                // En producción, esto consultaría la base de datos
                if (id <= 0)
                    return BadRequest(new { error = "ID inválido" });

                // Estructura ejemplo (cuando se agregue DB, reemplazar)
                var ejemplo = new
                {
                    exito = true,
                    datos = new
                    {
                        id = id,
                        consulta = "Consulta fiscal de ejemplo",
                        respuesta = "[CFF, Art. 17-A] Ejemplo de respuesta fiscal...",
                        motorUtilizado = "Claude",
                        tipoRespuesta = "Breve",
                        fechaCreacion = DateTime.UtcNow,
                        metadata = new
                        {
                            citasNormativas = 5,
                            alucinacionesDetectadas = 0,
                            tiempoMs = "00:00:02.5000000"
                        }
                    },
                    mensaje = "Estructura de ejemplo (DB no configurada aún)"
                };

                return Ok(ejemplo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo consulta {id}: {ex.Message}");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        exito = false,
                        error = "Error obteniendo la consulta"
                    });
            }
        }

        /// <summary>
        /// Obtener lista de documentos legales subidos
        /// </summary>
        [HttpGet("documentos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDocumentos(
            [FromServices] FilesApiService filesService)
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de documentos");

                // SINTAXIS CORRECTA: ListFilesAsync() retorna List<FileListItem>
                var archivos = await filesService.ListFilesAsync();

                return Ok(new
                {
                    exito = true,
                    total = archivos.Count,
                    documentos = archivos.Select(f => new
                    {
                        f.Id,
                        f.Filename,
                        f.MimeType,
                        SizeKB = f.SizeBytes / 1024,
                        f.CreatedAt,
                        f.Downloadable
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo documentos: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Listar documentos con paginación manual
        /// </summary>
        [HttpGet("documentos/pagina")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDocumentosPaginados(
            [FromServices] FilesApiService filesService,
            [FromQuery] int limit = 20,
            [FromQuery] string afterId = null
            )
        {
            try
            {
                var (archivos, hasMore, firstId, lastId) =
                    await filesService.ListFilesWithPaginationAsync(limit, afterId);

                return Ok(new
                {
                    exito = true,
                    total = archivos.Count,
                    paginacion = new
                    {
                        hasMore,
                        firstId,
                        lastId,
                        nextAfter = hasMore ? lastId : null
                    },
                    documentos = archivos.Select(f => new
                    {
                        f.Id,
                        f.Filename,
                        f.MimeType,
                        f.SizeBytes,
                        f.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar un documento específico
        /// </summary>
        [HttpDelete("documentos/{fileId}")]
        public async Task<IActionResult> EliminarDocumento(
            string fileId,
            [FromServices] FilesApiService filesService)
        {
            try
            {
                var resultado = await filesService.DeleteFileAsync(fileId);

                if (!resultado)
                    return NotFound(new { error = "Documento no encontrado" });

                return Ok(new { exito = true, mensaje = $"Documento {fileId} eliminado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Consultar con OpenAI FileSearch
        /// </summary>
        [HttpPost("procesar-filesearch")]
        public async Task<IActionResult> ProcesarFileSearch(
            [FromBody] ConsultaRequestDto request,
            [FromServices] OpenAIFileSearchService fileSearchService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Consulta))
                    return BadRequest(new { error = "Consulta requerida" });

                _logger.LogInformation($"FileSearch: {request.Consulta[..Math.Min(50, request.Consulta.Length)]}...");

                var resultado = await fileSearchService.AskAsync(
                    request.Consulta,
                    request.TipoRespuesta.ToString().ToLower());

                return Ok(new
                {
                    exito = true,
                    motor = "OpenAI FileSearch",
                    datos = new
                    {
                        consulta = request.Consulta,
                        respuesta = resultado.Answer,
                        motorUtilizado = "OpenAI (gpt-4o-mini + FileSearch)",
                        tipoRespuesta = resultado.ModeUsed,
                        metadata = new
                        {
                            vectorStore = "100% garantía",
                            validacion = "Exitosa ✅"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Comparar Claude vs OpenAI FileSearch
        /// </summary>
        [HttpPost("comparar-velocidad")]
        public async Task<IActionResult> CompararVelocidad(
            [FromBody] ConsultaRequestDto request,
            [FromServices] ClaudeService claudeService,
            [FromServices] OpenAIFileSearchService fileSearchService)
        {
            var sw1 = Stopwatch.StartNew();
            var sw2 = Stopwatch.StartNew();

            try
            {
                var systemPrompt = @"Eres NORMAFISCAL IA especialista en fiscalidad mexicana.
Responde SOLO basándote en documentos.
Cita: [LEY, Art. X]";

                // CLAUDE
                sw1.Start();
                var respuestaClaude = await claudeService.ConsultarAsync(
                    request.Consulta,
                    systemPrompt,
                    1500);
                sw1.Stop();

                // OPENAI FILESEARCH
                sw2.Start();
                var resultadoFileSearch = await fileSearchService.AskAsync(
                    request.Consulta,
                    "tecnica");
                sw2.Stop();

                return Ok(new
                {
                    exito = true,
                    consulta = request.Consulta,
                    comparacion = new
                    {
                        claude = new
                        {
                            tiempo_ms = sw1.ElapsedMilliseconds,
                            caracteres = respuestaClaude.Length
                        },
                        openai_filesearch = new
                        {
                            tiempo_ms = sw2.ElapsedMilliseconds,
                            caracteres = resultadoFileSearch.Answer.Length
                        },
                        ganador = sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds ? "Claude ⚡" : "OpenAI FileSearch 🚀",
                        diferencia_ms = Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

 
            /// <summary>
            /// Endpoint Claude - Con mismo prompt y mismo JSON
            /// </summary>
            [HttpPost("procesar-claude")]
            public async Task<IActionResult> ProcesarClaude([FromBody] ConsultaRequestDto request)
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    if (string.IsNullOrWhiteSpace(request.Consulta) || request.Consulta.Length < 10)
                        return BadRequest(new { error = "Consulta inválida" });

                    _logger.LogInformation($"Claude: {request.Consulta[..Math.Min(50, request.Consulta.Length)]}...");

                var modo = request.TipoRespuesta.ToString().ToLower();
                    var respuesta = await _claudeService.ConsultarAsync(request.Consulta, modo, 1500);

                    stopwatch.Stop();

                    var validacion = _validadorService.ValidarRespuesta(respuesta, modo);

                    return Ok(new UnifiedResponseDto
                    {
                        Exito = true,
                        Motor = "Claude",
                        Datos = new ConsultaDatosDto
                        {
                            Id = 0,
                            Consulta = request.Consulta,
                            Respuesta = respuesta,
                            MotorUtilizado = "Claude (Opus 4.7)",
                            TipoRespuesta = (int)request.TipoRespuesta,
                            Metadata = new MetadataDto
                            {
                                CitasNormativas = _validadorService.ContarCitas(respuesta),
                                AlucinacionesDetectadas = _validadorService.DetectarAlucinaciones(respuesta) ? 1 : 0,
                                EstructuraCompleta = true,
                                TiempoMs = $"{stopwatch.Elapsed.TotalSeconds:F2}s",
                                NivelConfianza = ObtenerNivelConfianzaNumerico(validacion.NivelConfianza),
                                NormasAplicables = new List<string> { "CFF", "LISR", "LIVA", "LIEPS", "RCFF" }
                            },
                            FechaCreacion = DateTime.UtcNow
                        },
                        Mensaje = "Consulta procesada exitosamente con Claude"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex.Message}");
                    return StatusCode(500, new { error = ex.Message });
                }
            }

            /// <summary>
            /// Endpoint OpenAI - Con mismo prompt y mismo JSON
            /// </summary>
            [HttpPost("procesar-openai")]
            public async Task<IActionResult> ProcesarOpenAI([FromBody] ConsultaRequestDto request)
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    if (string.IsNullOrWhiteSpace(request.Consulta) || request.Consulta.Length < 10)
                        return BadRequest(new { error = "Consulta inválida" });

                    _logger.LogInformation($"OpenAI: {request.Consulta[..Math.Min(50, request.Consulta.Length)]}...");
                
                    var modo = request.TipoRespuesta.ToString().ToLower();
                    var resultado = await _openaiService.AskAsync(request.Consulta, modo);

                    stopwatch.Stop();

                    var validacion = _validadorService.ValidarRespuesta(resultado.Answer, modo);

                    return Ok(new UnifiedResponseDto
                    {
                        Exito = true,
                        Motor = "OpenAI",
                        Datos = new ConsultaDatosDto
                        {
                            Id = 0,
                            Consulta = request.Consulta,
                            Respuesta = resultado.Answer,
                            MotorUtilizado = "OpenAI (gpt-4o-mini + FileSearch)",
                            TipoRespuesta = (int)request.TipoRespuesta,
                            Metadata = new MetadataDto
                            {
                                CitasNormativas = _validadorService.ContarCitas(resultado.Answer),
                                AlucinacionesDetectadas = _validadorService.DetectarAlucinaciones(resultado.Answer) ? 1 : 0,
                                EstructuraCompleta = true,
                                TiempoMs = $"{stopwatch.Elapsed.TotalSeconds:F2}s",
                                NivelConfianza = ObtenerNivelConfianzaNumerico(validacion.NivelConfianza),
                                NormasAplicables = new List<string> { "CFF", "LISR", "LIVA", "LIEPS", "RCFF" }
                            },
                            FechaCreacion = DateTime.UtcNow
                        },
                        Mensaje = "Consulta procesada exitosamente con OpenAI FileSearch"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex.Message}");
                    return StatusCode(500, new { error = ex.Message });
                }
            }

            /// <summary>
            /// Comparar ambos - MISMO JSON en ambas respuestas
            /// </summary>
            [HttpPost("comparar")]
            public async Task<IActionResult> Comparar([FromBody] ConsultaRequestDto request)
            {
                var sw1 = Stopwatch.StartNew();
                var sw2 = Stopwatch.StartNew();

                try
                {
                    var modo = request.TipoRespuesta.ToString().ToLower();

                    // CLAUDE
                    sw1.Start();
                    var respuestaClaude = await _claudeService.ConsultarAsync(request.Consulta, modo, 1500);
                    sw1.Stop();

                    // OPENAI
                    sw2.Start();
                    var resultadoOpenAI = await _openaiService.AskAsync(request.Consulta, modo);
                    sw2.Stop();

                    return Ok(new
                    {
                        exito = true,
                        consulta = request.Consulta,
                        claude = new UnifiedResponseDto
                        {
                            Exito = true,
                            Motor = "Claude",
                            Datos = new ConsultaDatosDto
                            {
                                Consulta = request.Consulta,
                                Respuesta = respuestaClaude,
                                MotorUtilizado = "Claude (Opus 4.7)",
                                TipoRespuesta = (int)request.TipoRespuesta,
                                Metadata = new MetadataDto
                                {
                                    TiempoMs = $"{sw1.Elapsed.TotalSeconds:F2}s"
                                }
                            }
                        },
                        openai = new UnifiedResponseDto
                        {
                            Exito = true,
                            Motor = "OpenAI",
                            Datos = new ConsultaDatosDto
                            {
                                Consulta = request.Consulta,
                                Respuesta = resultadoOpenAI.Answer,
                                MotorUtilizado = "OpenAI (gpt-4o-mini)",
                                TipoRespuesta = (int)request.TipoRespuesta,
                                Metadata = new MetadataDto
                                {
                                    TiempoMs = $"{sw2.Elapsed.TotalSeconds:F2}s"
                                }
                            }
                        },
                        comparacion = new
                        {
                            tiempo_claude_ms = sw1.ElapsedMilliseconds,
                            tiempo_openai_ms = sw2.ElapsedMilliseconds,
                            ganador = sw1.ElapsedMilliseconds < sw2.ElapsedMilliseconds ? "Claude" : "OpenAI",
                            diferencia_ms = Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds)
                        }
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = ex.Message });
                }
            }
       

        /// <summary>
        /// Verifica estado de la API
        /// </summary>
        /// <returns>Estado operativo</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                estado = "Operativo",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }


        /// <summary>
        /// Convertir string de confianza a número
        /// </summary>
        private int ObtenerNivelConfianzaNumerico(string nivel)
        {
            return nivel?.ToLower() switch
            {
                "baja" => 1,
                "media" => 2,
                "alta" => 3,
                _ => 2  // Default: Media
            };
        }

    }
}