using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Habilitar CORS por si tu frontend está en otro dominio o puerto durante las pruebas
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
app.UseStaticFiles(); // Sirve archivos estáticos (tu index.html si lo ponés en la carpeta wwwroot)

// Ruta del archivo JSON único donde se guardarán todas las inscripciones
string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "equipos.json");

// Endpoint para recibir la inscripción desde la página web
app.MapPost("/api/inscribir", async (HttpContext context) =>
{
    try
    {
        // Leer los datos que manda el formulario de la página
        var nuevoEquipo = await JsonSerializer.DeserializeAsync<EquipoModel>(context.Request.Body);

        if (nuevoEquipo == null || string.IsNullOrWhiteSpace(nuevoEquipo.NombreEquipo))
        {
            return Results.BadRequest(new { success = false, message = "Datos de equipo inválidos." });
        }

        // Cargar la lista existente de equipos desde el JSON (si ya existe)
        List<EquipoModel> equiposList = new();
        if (File.Exists(jsonFilePath))
        {
            string existingJson = await File.ReadAllTextAsync(jsonFilePath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                equiposList = JsonSerializer.Deserialize<List<EquipoModel>>(existingJson) ?? new();
            }
        }

        // Agregar fecha y hora de registro
        nuevoEquipo.FechaInscripcion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Agregar el nuevo equipo a la lista
        equiposList.Add(nuevoEquipo);

        // Guardar la lista actualizada de vuelta en el archivo JSON con formato legible
        var options = new JsonSerializerOptions { WriteIndented = true };
        string updatedJson = JsonSerializer.Serialize(equiposList, options);
        await File.WriteAllTextAsync(jsonFilePath, updatedJson);

        return Results.Ok(new { success = true, message = "¡Inscripción guardada con éxito en el JSON!" });
    }
    >catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Run();

// Modelo de datos que recibe el backend
public class EquipoModel
{
    public string NombreEquipo { get; set; } = string.Empty;
    public int CantidadIntegrantes { get; set; }
    public List<string> Integrantes { get; set; } = new();
    public string? FechaInscripcion { get; set; }
}