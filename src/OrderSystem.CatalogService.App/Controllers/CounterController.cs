// -----------------------------------------------------------------------
// <copyright file="CounterController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OrderSystem.CatalogService.App.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Akka.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using OrderSystem.CatalogService.App.Actors;
    using OrderSystem.Infrastructure.Controllers;

    [ApiController]
    [Route("[controller]")]
    public class CounterController : ControllerBase
    {
        private readonly BaseCounterController<CounterActor, Counter> baseController;

        public CounterController(ILogger<CounterController> logger, IRequiredActor<CounterActor> counterActor)
        {
            baseController = new InnerCounterController(logger, counterActor);
        }

        private class InnerCounterController : BaseCounterController<CounterActor, Counter>
        {
            public InnerCounterController(ILogger logger, IRequiredActor<CounterActor> counterActor)
                : base(logger, counterActor)
            {
            }
        }

        [HttpGet("{counterId}")]
        public async Task<Counter> Get(string counterId)
        {
            return await baseController.GetCounter(counterId);
        }

        [HttpPost("{counterId}")]
        public async Task<IActionResult> Post(string counterId, [FromBody] int increment)
        {
            var result = await baseController.IncrementCounter(counterId, increment);
            if (!result.IsSuccess)
            {
                return BadRequest();
            }

            return Ok(result.Event);
        }

        [HttpPut("{counterId}")]
        public async Task<IActionResult> Put(string counterId, [FromBody] int counterValue)
        {
            var result = await baseController.SetCounter(counterId, counterValue);
            if (!result.IsSuccess)
            {
                return BadRequest();
            }

            return Ok(result.Event);
        }
    }
}