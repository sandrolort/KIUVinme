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

builder.Services.AddDbContext<ChatContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
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

app.Run();