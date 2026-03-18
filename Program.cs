using YOUVI.RelayServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});

builder.Services.AddSignalR();
var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => "YOUVI Relay Server");

app.MapHub<RelayHub>("/webrtchub");

app.Run();
