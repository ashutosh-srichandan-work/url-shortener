using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Middleware;
using UrlShortener.Api.Services;
using UrlShortener.Domain.Interfaces;
using UrlShortener.Infrastructure.Data;
using UrlShortener.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
