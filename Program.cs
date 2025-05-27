using LojaManoel.Models;
using LojaManoel.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
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

// Registrar seus serviços customizados
builder.Services.AddScoped<IEmbalagemService, EmbalagemService>();

var app = builder.Build();

// Configurar o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loja do Seu Manoel API V1");
        // c.RoutePrefix = string.Empty; // Para acessar o Swagger UI na raiz da aplicação (opcional)
    });
}

// Aplicar migrações do banco de dados na inicialização
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Aplicando migrações do banco de dados...");
        context.Database.Migrate(); // Aplica quaisquer migrações pendentes
        logger.LogInformation("Migrações do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Um erro ocorreu ao aplicar migrações ou popular o banco de dados.");
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();