using KiuWho.Context;
using KiuWho.Hub;
using KiuWho.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddSingleton<ChatServices>();
builder.Services.AddScoped<StickerPackServices>();
builder.Services.AddScoped<StickerServices>();
builder.Services.AddScoped<MediaServices>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction())
{
    connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
}
builder.Services.AddDbContext<ChatContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatPolicy", policy =>
    {
        var allowedOrigins = builder.Environment.IsProduction() 
            ? Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ?? new[] { "*" }
            : new[] { "http://localhost:5000", "https://localhost:5001" };
        
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("ChatPolicy");
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");

app.MapControllers();

app.MapRazorPages();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");