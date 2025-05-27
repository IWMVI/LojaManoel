using LojaManoel.Models;
using LojaManoel.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao cont�iner.
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja do Seu Manoel API", Version = "v1" });
});

// Configurar DbContext com SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar seus servi�os customizados
builder.Services.AddScoped<IEmbalagemService, EmbalagemService>();

var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loja do Seu Manoel API V1");
        // c.RoutePrefix = string.Empty; // Para acessar o Swagger UI na raiz da aplica��o (opcional)
    });
}

// Aplicar migra��es do banco de dados na inicializa��o
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Aplicando migra��es do banco de dados...");
        context.Database.Migrate(); // Aplica quaisquer migra��es pendentes
        logger.LogInformation("Migra��es do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Um erro ocorreu ao aplicar migra��es ou popular o banco de dados.");
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();