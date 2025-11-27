using SupportTools.Server.Data;
using Microsoft.EntityFrameworkCore;
using SupportTools.Server.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Voeg services toe aan de container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SupportTools API", Version = "v1" });
});

// ✅ Voeg CORS toe vóór het bouwen van de app
builder.Services.AddCors(options => {
    options.AddPolicy("AllowBlazorClient", policy => {
        policy.WithOrigins("https://localhost:5176") // frontend-poort
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<SupportToolsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ExcelImportService>();

var app = builder.Build();

// ✅ Zorg dat de database wordt gemigreerd bij startup
using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<SupportToolsDbContext>();
    dbContext.Database.Migrate();
}

// Configureer de HTTP request pipeline
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ Gebruik CORS vóór routing
app.UseCors("AllowBlazorClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
