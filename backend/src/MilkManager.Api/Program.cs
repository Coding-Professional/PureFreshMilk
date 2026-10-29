using MilkManager.Api.Features.Status;
using MilkManager.Api.Hosting;
using MilkManager.Application;
using MilkManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddFrontendCors(builder.Configuration);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCors.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();

var api = app.MapGroup("/api/v1");
api.MapStatusEndpoints();

app.Run();
