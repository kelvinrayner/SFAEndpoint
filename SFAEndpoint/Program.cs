using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var secretKey = "F3ED67864EE948B4E23F6C168B46CQE2VIsAj>#ue#dT"; // Replace with a strong, secure key
var key = Encoding.UTF8.GetBytes(secretKey);

// Add services to the container
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // Skip validating the token's issuer
            ValidateAudience = false, // Skip validating the token's audience
            ValidateIssuerSigningKey = true, // Validate the signature
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true, // Ensure the token hasn't expired
            ClockSkew = TimeSpan.Zero // Optional: Eliminate clock skew
        };
    });

builder.Services.AddAuthorization(); // Add authorization services
builder.Services.AddControllers();   // Add controllers for handling API requests

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
