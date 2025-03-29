using Microsoft.AspNetCore.Mvc;
using StocksProcessing.Streamer.Streamers;

namespace StocksProcessing.Streamer.Controllers;

[ApiController]
public class CurrenciesStatisticsEventsController(
    CurrenciesStatisticsProducer producer)
    : ControllerBase
{
    [Route("/ws/currencies/statistics")]
    public async Task GetCurrencyStatisticsEventsAsync()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await producer.SubscribeToCurrencyStatisticsEventsAsync(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}