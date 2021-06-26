using Grpc.Net.Client;
using GrpcConsole;
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
            services.AddControllersWithViews();

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
                
                return  new GameService.GameServiceClient(channel);
            });
            services.AddScoped(col =>
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        //.AddApplicationInsights(ikey)
                        .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                        .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                        .AddFilter("LoggingConsoleApp.Program", Microsoft.Extensions.Logging.LogLevel.Debug)
                        //.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>
                        //     ("", Microsoft.Extensions.Logging.LogLevel.Trace)
                        .AddConsole();
                    ;
                });

                var logger = loggerFactory.CreateLogger("main");
               // var logger = col.GetRequiredService<ILogger>();
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

            app.UseRouting();

            app.UseCors(builder => builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseFileServer();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GameHub>("/game");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
