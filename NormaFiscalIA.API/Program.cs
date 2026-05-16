using Serilog;
using NormaFiscalIA.Services.Interfaces;
using NormaFiscalIA.Services.Services;
using NormaFiscalIA.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("All", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IRouterIA, RouterIA>();
builder.Services.AddScoped<IValidadorService, ValidadorService>();
builder.Services.AddHttpClient();

// Agregar servicios
builder.Services.AddScoped<FilesApiService>();
builder.Services.AddScoped<ClaudeService>();

//Servicios OpenAI
// CONFIGURAR SETTINGS
builder.Services.Configure<OpenAiSettings>(
builder.Configuration.GetSection("APIs:OpenAI"));
builder.Services.AddScoped<OpenAIFileSearchService>();

// REGISTRAR SERVICIO UNIFICADO DE PROMPTS
builder.Services.AddScoped<UnifiedPromptService>();
builder.Services.AddScoped<ClaudeService>();
builder.Services.AddScoped<OpenAIFileSearchService>();



var app = builder.Build();

// Inicializar documentos al startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var claudeService = scope.ServiceProvider.GetRequiredService<ClaudeService>();
        //await claudeService.InitializeDocumentsAsync();
    }
    catch (Exception ex)
    {
        Log.Error($"Error inicializando: {ex.Message}");
    }
}

//var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors("All");
app.MapControllers();

Log.Information("NormaFiscal IA iniciando...");

try { app.Run(); }
catch (Exception ex) { Log.Fatal(ex, "Aplicación terminó"); }
finally { Log.CloseAndFlush(); }
