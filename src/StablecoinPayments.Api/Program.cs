using StablecoinPayments.Api.Middleware;
using StablecoinPayments.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Stablecoin Payments API", Version = "v1" });
});

// Add payment services
builder.Services.AddPaymentServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandling();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
