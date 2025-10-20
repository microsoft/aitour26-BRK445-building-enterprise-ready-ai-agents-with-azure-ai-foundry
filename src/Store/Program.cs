using Store.Components;
using Store.Services;

var builder = WebApplication.CreateBuilder(args);

// add aspire service defaults
builder.AddServiceDefaults();

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<AgentFrameworkService>();

builder.Services.AddHttpClient<IProductService, ProductService>(
    static client => client.BaseAddress = new("https+http://products"));

builder.Services.AddHttpClient<SingleAgentService>(
    static client => client.BaseAddress = new("https+http://singleagentdemo"));

builder.Services.AddHttpClient<MultiAgentService>(
    static client => client.BaseAddress = new("https+http://multiagentdemo"));

// Add agent tester service that connects to the AgentsCatalogService
builder.Services.AddHttpClient<AgentCatalogService>(
    static client => client.BaseAddress = new("https+http://agentscatalogservice"));

// blazor bootstrap
builder.Services.AddBlazorBootstrap();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// aspire map default endpoints
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
