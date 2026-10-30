using PureFreshMilk.Api.Features.Households;
using PureFreshMilk.Api.Features.MilkRates;
using PureFreshMilk.Api.Features.Recipients;
using PureFreshMilk.Api.Features.Status;
using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application;
using PureFreshMilk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
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

var api = app.MapGroup(ApiRoutes.Prefix);
api.MapStatusEndpoints();

// These endpoints read and write a household without a login, so they exist only where that is safe.
if (UnauthenticatedAccess.IsAllowed(app.Environment, app.Configuration))
{
    if (UnauthenticatedAccess.IsExplicitOptIn(app.Environment, app.Configuration))
    {
        app.Logger.LogWarning(
            "{Setting} is true: the data endpoints are open to anyone who can reach this API. " +
            "Do not expose it to a network you do not trust until authentication exists.",
            UnauthenticatedAccess.SettingName);
    }

    api.MapHouseholdEndpoints();
    api.MapRecipientEndpoints();
    api.MapMilkRateEndpoints();
}

app.Run();
