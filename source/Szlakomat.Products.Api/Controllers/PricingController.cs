using MediatR;
using Microsoft.AspNetCore.Mvc;
using Szlakomat.Pricing.Application.CalculatePrice;
using Szlakomat.Products.Api.Contracts.Pricing;
using Szlakomat.Products.Api.Mappers;

namespace Szlakomat.Products.Api.Controllers;

/// <summary>
/// Obliczenia cen z modułu pricingu (bounded context Pricing).
/// </summary>
[ApiController]
[Route("api/pricing")]
[Produces("application/json")]
public class PricingController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Oblicza cenę dla wpisu katalogowego i kontekstu wizyty.
    /// </summary>
    /// <param name="request">Identyfikator wpisu katalogowego, data wizyty, typ klienta i wielkość grupy.</param>
    /// <response code="200">Obliczona cena (może być 0 PLN, gdy żaden wariant taryfy nie pasuje).</response>
    /// <response code="400">Błąd walidacji, brak wpisu, brak konfiguracji pricingu lub brak aktywnej wersji cennika.</response>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(CalculatePriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Calculate([FromBody] CalculatePriceRequest request)
    {
        var command = new CalculatePrice(
            request.CatalogEntryId,
            DateOnly.Parse(request.VisitDate),
            request.CustomerType,
            request.GroupSize,
            request.IncludeBreakdown);

        var result = await mediator.Send(command);
        if (!result.IsSuccess())
            return BadRequest(new { error = result.GetFailure() });

        return Ok(PricingMapper.ToResponse(result.SuccessValue));
    }
}
