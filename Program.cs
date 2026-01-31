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

// 🔐 Auth services (ADD THESE 👇)
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

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

app.UseHttpsRedirection();

// (JWT auth will be added next)
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
