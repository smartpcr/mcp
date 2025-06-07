// -----------------------------------------------------------------------
// <copyright file="AkkaConfiguration.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Infrastructure.Configuration
{
    using System;
    using System.Diagnostics;
    using Akka.Actor;
    using Akka.Cluster.Hosting;
    using Akka.Cluster.Sharding;
    using Akka.Configuration;
    using Akka.Discovery.Azure;
    using Akka.Discovery.Config.Hosting;
    using Akka.Hosting;
    using Akka.Management;
    using Akka.Persistence.Azure;
    using Akka.Persistence.Azure.Hosting;
    using Akka.Persistence.Hosting;
    using Akka.Remote.Hosting;
    using Akka.Util;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using OrderSystem.Contracts.Messages;
    using OrderSystem.Infrastructure.Actors;

    public static class AkkaConfiguration
    {
        public static IServiceCollection ConfigureOrderSystemAkka(this IServiceCollection services, IConfiguration configuration,
            Action<AkkaConfigurationBuilder, IServiceProvider> additionalConfig)
        {
            var akkaSettings = configuration.GetRequiredSection("AkkaSettings").Get<AkkaSettings>();
            Debug.Assert(akkaSettings != null, nameof(akkaSettings) + " != null");

            services.AddSingleton(akkaSettings);

            return services.AddAkka(akkaSettings.ActorSystemName, (builder, sp) =>
            {
                builder.ConfigureActorSystem(sp);
                additionalConfig(builder, sp);
            });
        }

        public static AkkaConfigurationBuilder ConfigureActorSystem(this AkkaConfigurationBuilder builder,
            IServiceProvider sp)
        {
            var settings = sp.GetRequiredService<AkkaSettings>();

            return builder
                .ConfigureLoggers(configBuilder =>
                {
                    configBuilder.LogConfigOnStart = settings.LogConfigOnStart;
                    configBuilder.AddLoggerFactory();
                })
                .ConfigureNetwork(sp)
                .ConfigurePersistence(sp);
        }

        public static AkkaConfigurationBuilder ConfigureNetwork(this AkkaConfigurationBuilder builder,
            IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<AkkaSettings>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            if (!settings.UseClustering)
                return builder;

            builder
                .WithRemoting(settings.RemoteOptions);

            if (settings.AkkaManagementOptions is { Enabled: true })
            {
                // need to delete seed-nodes so Akka.Management will take precedence
                var clusterOptions = settings.ClusterOptions;
                clusterOptions.SeedNodes = Array.Empty<string>();

                builder
                    .WithClustering(clusterOptions)
                    .WithAkkaManagement(hostName: settings.AkkaManagementOptions.Hostname,
                        settings.AkkaManagementOptions.Port);

                switch (settings.AkkaManagementOptions.DiscoveryMethod)
                {
                    case DiscoveryMethod.Kubernetes:
                        break;
                    case DiscoveryMethod.AwsEcsTagBased:
                        break;
                    case DiscoveryMethod.AwsEc2TagBased:
                        break;
                    case DiscoveryMethod.AzureTableStorage:
                    {
                        var connectionStringName = configuration.GetSection("AzureStorageSettings")
                            .Get<AzureStorageSettings>()?.ConnectionStringName;
                        Debug.Assert(connectionStringName != null, nameof(connectionStringName) + " != null");
                        var connectionString = configuration.GetConnectionString(connectionStringName);

                        builder.WithAzureDiscovery(options =>
                        {
                            options.ServiceName = settings.AkkaManagementOptions.ServiceName;
                            options.ConnectionString = connectionString;
                        });
                        break;
                    }
                    case DiscoveryMethod.Config:
                    {
                        builder
                            .WithConfigDiscovery(options =>
                            {
                                options.Services.Add(new Service
                                {
                                    Name = settings.AkkaManagementOptions.ServiceName,
                                    Endpoints = new[]
                                    {
                                        $"{settings.AkkaManagementOptions.Hostname}:{settings.AkkaManagementOptions.Port}",
                                    }
                                });
                            });
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.WithClustering(settings.ClusterOptions);
            }

            return builder;
        }

        public static AkkaConfigurationBuilder ConfigurePersistence(this AkkaConfigurationBuilder builder,
            IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<AkkaSettings>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            switch (settings.PersistenceMode)
            {
                case PersistenceMode.InMemory:
                    return builder.WithInMemoryJournal().WithInMemorySnapshotStore();
                case PersistenceMode.Azure:
                {
                    var connectionStringName = configuration.GetSection("AzureStorageSettings")
                        .Get<AzureStorageSettings>()?.ConnectionStringName;
                    Debug.Assert(connectionStringName != null, nameof(connectionStringName) + " != null");
                    var connectionString = configuration.GetConnectionString(connectionStringName);
                    Debug.Assert(connectionString != null, nameof(connectionString) + " != null");

                    return builder.WithAzurePersistence(connectionString);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static AkkaConfigurationBuilder ConfigureShardedActors<TActor>(this AkkaConfigurationBuilder builder,
            string regionName,
            Func<string, Props> actorPropsFactory,
            HashCodeMessageExtractor messageExtractor,
            IServiceProvider serviceProvider)
            where TActor : ActorBase
        {
            var settings = serviceProvider.GetRequiredService<AkkaSettings>();

            if (settings.UseClustering)
            {
                return builder.WithShardRegion<TActor>(regionName,
                    (system, registry, resolver) => actorPropsFactory,
                    messageExtractor, settings.ShardOptions);
            }

            return builder.WithActors((system, registry, resolver) =>
            {
                var parent = system.ActorOf(
                    GenericChildPerEntityParent.Props(messageExtractor, actorPropsFactory),
                    regionName);
                registry.Register<TActor>(parent);
            });
        }

        public static HashCodeMessageExtractor CreateMessageExtractor<TMessage>(int maxShards, Func<object, string?> entityIdExtractor)
        {
            return HashCodeMessageExtractor.Create(maxShards, entityIdExtractor, o => o);
        }
    }
}