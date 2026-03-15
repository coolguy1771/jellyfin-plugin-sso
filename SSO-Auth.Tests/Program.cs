using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
