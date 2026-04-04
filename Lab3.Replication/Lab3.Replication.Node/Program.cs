using Lab3.Replication.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var nodeId = builder.Configuration["NodeId"] ?? "node-unknown";
var isMaster = bool.Parse(builder.Configuration["IsMaster"] ?? "false");

builder.Services.AddSingleton(new InMemoryStore(nodeId, isMaster));
builder.Services.AddSingleton<NodeHealthService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ReplicationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = $"Lab3 - Replication Node [{nodeId}]",
        Version = "v1",
        Description = $"Узел: {nodeId}, Роль: {(isMaster ? "MASTER" : "REPLICA")}"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Node [{nodeId}]");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();

namespace Lab3.Replication
{
    public partial class Program { }
}