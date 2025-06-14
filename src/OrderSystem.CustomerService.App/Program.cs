// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CustomerService.App
{
    using System;
    using Akka.HealthCheck.Hosting;
    using Akka.HealthCheck.Hosting.Web;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using OrderSystem.Infrastructure.Configuration;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            /*
             * CONFIGURATION SOURCES
             */
            builder.Configuration
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();

            // Add services to the container.
            builder.Services.WithAkkaHealthCheck(HealthCheckType.All);
            builder.Services.ConfigureOrderSystemAkka(builder.Configuration, (akkaConfigurationBuilder, serviceProvider) =>
            {
                // we configure instrumentation separately from the internals of the ActorSystem
                akkaConfigurationBuilder.ConfigurePetabridgeCmd();
                akkaConfigurationBuilder.WithWebHealthCheck(serviceProvider);
            });

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Azure"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.MapAkkaHealthCheckRoutes(optionConfigure: (_, opt) =>
            {
                // Use a custom response writer to output a json of all reported statuses
                opt.ResponseWriter = Helper.JsonResponseWriter;
            }); // needed for Akka.HealthCheck
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}