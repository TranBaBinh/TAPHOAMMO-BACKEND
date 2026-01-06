using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TAPHOAMMO_BACKEND.Data;
using TAPHOAMMO_BACKEND.Services;

var builder = WebApplication.CreateBuilder(args);

// Add User Secrets for Development environment
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TAPHOAMMO-BACKEND";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TAPHOAMMO-BACKEND";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(

                 "http://localhost:4200",
                 "https://localhost:4200",
                 "http://localhost:5173",
                 "https://taphoammo-eight.vercel.app",
                 "https://dev.bachhoammo.com.vn",
                 "https://api.bachhoammo.com.vn"
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddHttpContextAccessor(); // Cần cho ActivityLogService

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TAPHOAMMO API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
