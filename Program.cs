using UmbracoBase.Core.Bundling;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

// Add MVC services for custom controllers
builder.Services.AddControllersWithViews();

// Combine and minify the front-end stylesheets into one fingerprinted bundle.
builder.Services.AddSiteStylesheetBundle(builder.Environment);

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Must run before static files so /css/site.bundle.css is served by WebOptimizer.
app.UseWebOptimizer();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

// Configure custom controller routes
app.MapControllerRoute(
    name: "admin",
    pattern: "admin",
    defaults: new { controller = "Admin", action = "Index" });

// Add a catch-all route for our custom controllers
app.MapControllerRoute(
    name: "customControllers",
    pattern: "{controller}/{action=Index}/{id?}");

await app.RunAsync();
