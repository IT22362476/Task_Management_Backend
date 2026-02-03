using Microsoft.EntityFrameworkCore;
using Task_Manager_Backend.Data;
using Task_Manager_Backend.Helpers;
using Task_Manager_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

//
// 🔹 Services
//

// Controllers
builder.Services.AddControllers();

// PostgreSQL + EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// 🔐 Auth services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

// 🌐 CORS (Angular)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Swagger (OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//
// 🔹 Build app
//
var app = builder.Build();

//
// 🔹 Middleware pipeline
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// ⭐ CORS MUST come BEFORE auth
app.UseCors("AllowAngular");

// (JWT auth will be added later)
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
