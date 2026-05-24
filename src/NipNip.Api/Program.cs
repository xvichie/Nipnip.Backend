using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NipNip.Api.Auth;
using NipNip.Api.Email;
using NipNip.Api.Middleware;
using NipNip.Shared.Email;
using NipNip.Data;
using NipNip.Modules.Codes.Extensions;
using NipNip.Modules.Creators.Extensions;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Modules.Payouts.Extensions;
using NipNip.Modules.Tracking.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(NipNip.Modules.Codes.CodeController).Assembly)
    .AddApplicationPart(typeof(NipNip.Modules.Merchants.MerchantController).Assembly)
    .AddApplicationPart(typeof(NipNip.Modules.Creators.CreatorController).Assembly)
    .AddApplicationPart(typeof(NipNip.Modules.Tracking.RedirectController).Assembly)
    .AddApplicationPart(typeof(NipNip.Modules.Payouts.PayoutController).Assembly)
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddCodeModule();
builder.Services.AddMerchantModule();
builder.Services.AddCreatorModule();
builder.Services.AddTrackingModule();
builder.Services.AddPayoutModule();

var frontendOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());

    options.AddPolicy("Public", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        var defaultScheme = builder.Environment.IsDevelopment()
            ? "DevOrBearer"
            : JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = defaultScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Clerk:Authority"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            NameClaimType = "sub"
        };
    });

if (builder.Environment.IsDevelopment())
{
    authBuilder
        .AddPolicyScheme("DevOrBearer", "DevOrBearer", opts =>
            opts.ForwardDefaultSelector = ctx =>
                ctx.Request.Headers.ContainsKey("X-Dev-UserId")
                    ? "Dev"
                    : JwtBearerDefaults.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("Dev", _ => { });
}

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
