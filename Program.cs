using LojaManoel.Models;
using LojaManoel.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja do Seu Manoel API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando o esquema Bearer. <br/>
                      Entre com 'Bearer' [espaço] e então seu token na caixa de texto abaixo. <br/>
                      Exemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configurar Autenticação JWT Bearer
var secretKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    // Em produção, isso deve lançar uma exceção ou impedir a inicialização.
    Console.WriteLine("AVISO: Chave JWT não configurada ou muito curta. Usando chave padrão para DESENVOLVIMENTO.");
    secretKey = "YourSuperSecretKeyThatIsLongEnoughForHS256!DevOnly!";
}
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateIssuer = builder.Environment.IsProduction(),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = builder.Environment.IsProduction(),
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ClockSkew = TimeSpan.Zero // Opcional: remove a tolerância de tempo padrão
    };
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
    });
    app.UseDeveloperExceptionPage(); // Útil para desenvolvimento
}
else
{
    app.UseHsts();
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
        context.Database.Migrate();
        logger.LogInformation("Migrações do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Um erro ocorreu ao aplicar migrações ou popular o banco de dados.");
    }
}

app.UseHttpsRedirection();

// Middlewares de Roteamento, Autenticação e Autorização
// A ORDEM É IMPORTANTE AQUI!
app.UseRouting(); // Adicionado explicitamente para clareza, embora muitas vezes implícito.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();