using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Habilitar CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles(); // Carga index.html por defecto al entrar a la raíz
app.UseStaticFiles();  // Sirve los archivos de la carpeta wwwroot

// Ruta del archivo JSON único de base de datos
string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "equipos.json");

// Endpoint API para recibir inscripciones
app.MapPost("/api/inscribir", async (HttpContext context) =>
{
    try
    {
        var nuevoEquipo = await JsonSerializer.DeserializeAsync<EquipoModel>(context.Request.Body);

        if (nuevoEquipo == null || string.IsNullOrWhiteSpace(nuevoEquipo.NombreEquipo))
        {
            return Results.BadRequest(new { success = false, message = "Datos de equipo inválidos." });
        }

        List<EquipoModel> equiposList = new();

        // Si ya existe el JSON, lo leemos para agregar el nuevo equipo
        if (File.Exists(jsonFilePath))
        {
            string existingJson = await File.ReadAllTextAsync(jsonFilePath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                equiposList = JsonSerializer.Deserialize<List<EquipoModel>>(existingJson) ?? new();
            }
        }

        // Asignamos la fecha de inscripción
        nuevoEquipo.FechaInscripcion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Agregamos el nuevo equipo
        equiposList.Add(nuevoEquipo);

        // Guardamos todo de nuevo en el archivo JSON
        var options = new JsonSerializerOptions { WriteIndented = true };
        string updatedJson = JsonSerializer.Serialize(equiposList, options);
        await File.WriteAllTextAsync(jsonFilePath, updatedJson);

        return Results.Ok(new { success = true, message = "¡Inscripción guardada con éxito!" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Run();

// Modelo del equipo
public class EquipoModel
{
    public string NombreEquipo { get; set; } = string.Empty;
    public int CantidadIntegrantes { get; set; }
    public List<string> Integrantes { get; set; } = new();
    public string? FechaInscripcion { get; set; }
}