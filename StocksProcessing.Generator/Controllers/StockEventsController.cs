using Microsoft.AspNetCore.Mvc;
using StocksProcessing.Generator.Generators;

namespace StocksProcessing.Generator.Controllers;

[ApiController]
public class StockEventsController(
    CurrencyRateChangeEventsGenerator generator)
    : ControllerBase
{
    [Route("/ws/stocks/currencies")]
    public async Task GetCurrencyRateEventsAsync()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await generator.SubscribeToCurrencyChangeEventsAsync(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}