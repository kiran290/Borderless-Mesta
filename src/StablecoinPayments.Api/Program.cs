using StablecoinPayments.Api.Middleware;
using StablecoinPayments.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger WITHOUT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Stablecoin Payments API",
        Version = "v1"
    });
});

// Add payment services
builder.Services.AddPaymentServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stablecoin Payments API v1");
        c.RoutePrefix = string.Empty; // Swagger opens at root: https://localhost:xxxx/
    });
}

// Global exception handling middleware
app.UseExceptionHandling();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
}));

app.Run();
