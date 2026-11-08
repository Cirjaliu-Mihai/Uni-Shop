using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
public sealed class AddressesController(IAddressService addressService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AddressDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return userId is null ? Unauthorized() : Ok(await addressService.GetAddressesAsync(userId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AddressDto>> CreateAddress(AddressWriteRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var address = await addressService.CreateAddressAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAddresses), new { id = address.Id }, address);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(int id, AddressWriteRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var address = await addressService.UpdateAddressAsync(userId, id, request, cancellationToken);
        return address is null ? NotFound() : Ok(address);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return await addressService.DeleteAddressAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPut("{id:int}/default")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> SetDefault(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var address = await addressService.SetDefaultAsync(userId, id, cancellationToken);
        return address is null ? NotFound() : Ok(address);
    }

    private string? GetUserId() => User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}