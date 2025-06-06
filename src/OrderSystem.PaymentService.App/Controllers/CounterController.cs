// -----------------------------------------------------------------------
// <copyright file="CounterController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.PaymentService.App.Controllers;

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderSystem.Contracts.Messages;
using OrderSystem.PaymentService.App.Actors;

[ApiController]
[Route("[controller]")]
public class CounterController : ControllerBase
{
    private readonly ILogger<CounterController> _logger;
    private readonly IActorRef _counterActor;

    public CounterController(ILogger<CounterController> logger, IRequiredActor<CounterActor> counterActor)
    {
        _logger = logger;
        _counterActor = counterActor.ActorRef;
    }

    [HttpGet("{counterId}")]
    public async Task<OrderSystem.PaymentService.App.Actors.Counter> Get(string counterId)
    {
        var counter = await _counterActor.Ask<OrderSystem.PaymentService.App.Actors.Counter>(new FetchCounter(counterId), TimeSpan.FromSeconds(5));
        return counter;
    }

    [HttpPost("{counterId}")]
    public async Task<IActionResult> Post(string counterId, [FromBody] int increment)
    {
        var result = await _counterActor.Ask<CounterCommandResponse>(new IncrementCounterCommand(counterId, increment), TimeSpan.FromSeconds(5));
        if (!result.IsSuccess)
        {
            return BadRequest();
        }

        return Ok(result.Event);
    }

    [HttpPut("{counterId}")]
    public async Task<IActionResult> Put(string counterId, [FromBody] int counterValue)
    {
        var result = await _counterActor.Ask<CounterCommandResponse>(new SetCounterCommand(counterId, counterValue), TimeSpan.FromSeconds(5));
        if (!result.IsSuccess)
        {
            return BadRequest();
        }

        return Ok(result.Event);
    }
}