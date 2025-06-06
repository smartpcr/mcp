// -----------------------------------------------------------------------
// <copyright file="PetabridgeCmdConfiguration.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.OrderService.App.Configuration;

using Akka.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;

public static class PetabridgeCmdConfiguration
{
    public static AkkaConfigurationBuilder ConfigurePetabridgeCmd(this AkkaConfigurationBuilder builder)
    {
        return builder.AddPetabridgeCmd(cmd =>
        {
            cmd.RegisterCommandPalette(ClusterCommands.Instance);
            cmd.RegisterCommandPalette(new RemoteCommands());
            cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
        });
    }
}