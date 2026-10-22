using Battleship.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://telegrram.me", "https://mililks.github.io")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Обов'язково для SignalR
    });
});

// ... перед app.MapHub<GameHub>("/gamehub"):


var app = builder.Build();

app.UseCors();

app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "Battleship Server is running!");

app.Run();