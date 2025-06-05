// -----------------------------------------------------------------------
// <copyright file="ICommand.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.Core.Messages
{
    public interface ICommand
    {
        string CorrelationId { get; }
    }
}