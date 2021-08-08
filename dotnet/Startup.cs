using Grpc.Net.Client;
using TeamHitori.Mulplay.Container.Web.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Shared.Poco;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace TeamHitori.Mulplay.Container.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddControllersWithViews();

            var azureSignalrConnectionString = Configuration["Azure:SignalR:ConnectionString"];
            services.AddSignalR()
                .AddAzureSignalR(options =>
                {
                    options.ConnectionString = azureSignalrConnectionString;
                });
            services.AddSingleton<GameContainer>();
            services.AddSingleton(_=>
            {
                var gameContainerUri = Configuration["game_container_uri"];
                var channel = GrpcChannel.ForAddress(gameContainerUri);
                
                return new GameService.GameServiceClient(channel);
            });

            services.AddSingleton(x =>
            {
                var ikey = Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddApplicationInsights(ikey)
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                        .AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                             ("", LogLevel.Trace)
                             .AddConsole();
                });

                var logger = loggerFactory.CreateLogger("main");

                return logger;
            });


            services.AddScoped(col =>
            {

                //var loggerFactory = LoggerFactory.Create(builder =>
                //{
                //    builder
                //        .AddApplicationInsights(ikey)
                //        .AddFilter("Microsoft", LogLevel.Warning)
                //        .AddFilter("System", LogLevel.Warning)
                //        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                //        .AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                //             ("", LogLevel.Trace)
                //        .AddConsole();
                //    ;
                //});

                //var logger = loggerFactory.CreateLogger("main");
                var logger = col.GetRequiredService<ILogger>();
                var blobConnectionString = Configuration["Azure:Blob:ConnectionString"];
                var containerName = Configuration["Azure:Blob:ContainerName"];
                var cacheConnectionString = Configuration["Azure:Redis:ConnectionString"];
                var endpoint = Configuration["Azure:Cosmos:Endpoint"];
                var key = Configuration["Azure:Cosmos:Key"];
                var databaseId = Configuration["Azure:Cosmos:DatabaseId"];
                var collectionId = Configuration["Azure:Cosmos:CollectionId"];
                var cache = string.IsNullOrEmpty(cacheConnectionString)? null : ConnectionMultiplexer.Connect(cacheConnectionString).GetDatabase();

                return StorageExtensions.CreateStorage(blobConnectionString, endpoint, key, databaseId, collectionId, logger, cache);
            });

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();


            // Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });


            services.AddOptions();

            //services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "AzureAdB2CSignUp", openIdConnectScheme: "signup", cookieScheme: "signup-cookies")
            //        .EnableTokenAcquisitionToCallDownstreamApi(new string[] { Configuration["Scope_Member_Edit"] })
            //        .AddInMemoryTokenCaches();

            //var builder = services.AddAuthentication("");

            services.AddMicrosoftIdentityWebAppAuthentication(Configuration, "AzureAdB2C")
                    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { Configuration["Scope"] })
                    .AddInMemoryTokenCaches();

            services.AddControllersWithViews().AddMicrosoftIdentityUI();

            services.AddRazorPages();

            //Configuring appsettings section AzureAdB2C, into IOptions
            services.AddOptions();
            services.Configure<OpenIdConnectOptions>(Configuration.GetSection("AzureAdB2C"));

            // debuging

            //var section =
            //Configuration.GetSection("AzureAdB2C") as OpenIdConnectOptions;

            

            //

            services.Configure<WebEncoderOptions>(options =>
            {
                options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                var scheme = context.Request.Scheme;
                if(scheme == "http")
                {
                    context.Request.Scheme =  "https";
                }

                logger.LogInformation($"Middleware: {context.Request.Scheme}://{context.Request.Host}");
                

                // Call the next delegate/middleware in the pipeline
                await next();
            });


            app.UseRouting();

            app.UseCors(builder => builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseFileServer();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GameHub>("/game");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "editor",
                    pattern: "editor/{gameName?}",
                    defaults: new { controller = "editor", action = "Index" });

                endpoints.MapRazorPages();
            });

        }
    }
}
