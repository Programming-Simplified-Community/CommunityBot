using Dashboard.Services;
using Data.Challenges;
using Infrastructure;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMudServices();

builder.Services.AddScoped<IBasicCrudService<ProgrammingChallenge, int>, ChallengeService>();
builder.Services.AddScoped<IBasicCrudService<ProgrammingTest, int>, BasicCrudService<ProgrammingTest, int>>();
builder.Services.AddScoped<IBasicCrudService<ProgrammingChallengeReport, int>, BasicCrudService<ProgrammingChallengeReport, int>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();