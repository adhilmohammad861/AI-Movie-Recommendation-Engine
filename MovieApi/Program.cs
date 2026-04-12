using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ── Database (PostgreSQL + pgvector) ─────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        o => o.UseVector()
    )
);

// ── OpenAI client ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton(new OpenAIClient(
    builder.Configuration["OpenAI:ApiKey"]!
));

// ── CORS — allow React dev server ────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

builder.Services.AddControllers();

var app = builder.Build();

// ── Auto-create table + extension on startup ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE EXTENSION IF NOT EXISTS vector;

        CREATE TABLE IF NOT EXISTS movies (
            id          SERIAL PRIMARY KEY,
            title       TEXT NOT NULL,
            genre       TEXT NOT NULL,
            description TEXT NOT NULL,
            embedding   vector(1536),
            created_at  TIMESTAMPTZ DEFAULT NOW()
        );
        """);
}

app.UseCors();
app.MapControllers();
app.Run();
