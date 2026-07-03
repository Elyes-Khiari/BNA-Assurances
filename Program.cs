using AssuranceApp.Data;
using AssuranceApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}


builder.Configuration.AddEnvironmentVariables();


var groqApiKey = builder.Configuration["Groq:ApiKey"];
var supabaseKey = builder.Configuration["Supabase:ServiceKey"];

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseInMemoryDatabase("AssuranceApp"));
builder.Services.AddScoped<ReclamationService>();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
