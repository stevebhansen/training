var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Add Grok service
var grokApiKey = builder.Configuration["GrokAPI:ApiKey"] ?? "your_api_key_here"; // Get from configuration
builder.Services.AddSingleton<backend.Services.IGrokService>(provider => 
    new backend.Services.GrokService(grokApiKey));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add static file support
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});

// Set specific port (5296)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5296, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAngular");
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.UseRouting();
app.MapControllers();

// In production, serve the Angular app
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseSpaStaticFiles();
    
    app.UseSpa(spa =>
    {
        spa.Options.SourcePath = "wwwroot";
    });
}

app.Run();