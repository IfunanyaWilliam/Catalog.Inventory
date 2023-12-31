using System;
using System.Net.Http;
using Catalog.Common.MongoDB;
using Catalog.Inventory.Service.Clients;
using Catalog.Inventory.Service.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Timeout;

namespace Catalog.Inventory.Service
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
            services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryItems");


            //Add the CatalogAPI as a client
            services.AddHttpClient<CatalogClient>(client => 
            {
                client.BaseAddress = new Uri("https://localhost:5001");
            })
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                    5, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))       //Adds the exponential retry time
            ))
            .AddTransientHttpErrorPolicy(builder => 
                    builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                        3,
                        TimeSpan.FromSeconds(15),
                        onBreak: (outcome, timespan) =>
                        {
                            var serviceProvider = services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                                .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
                        },
                        onReset: () => 
                        {
                            var serviceProvider = services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                                .LogWarning($"Closig the circuit");
                        }
            ))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1)); //Timeout of 1 second for any request to CatalogAPI

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.Inventory.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.Inventory.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
