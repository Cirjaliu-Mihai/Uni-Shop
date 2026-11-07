using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public sealed class AdminReferenceDataController(IAdminReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryAdminDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetCategoriesAsync(cancellationToken));

    [HttpGet("categories/{id:int}")]
    [ProducesResponseType<CategoryAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryAdminDto>> GetCategory(int id, CancellationToken cancellationToken)
    {
        var category = await referenceDataService.GetCategoryAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost("categories")]
    [ProducesResponseType<CategoryAdminDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryAdminDto>> CreateCategory(CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var category = await referenceDataService.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType<CategoryAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryAdminDto>> UpdateCategory(int id, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var category = await referenceDataService.UpdateCategoryAsync(id, request, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken) =>
        await referenceDataService.DeleteCategoryAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("suppliers")]
    [ProducesResponseType<IReadOnlyList<SupplierAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierAdminDto>>> GetSuppliers(CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetSuppliersAsync(cancellationToken));

    [HttpGet("suppliers/{id:int}")]
    [ProducesResponseType<SupplierAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierAdminDto>> GetSupplier(int id, CancellationToken cancellationToken)
    {
        var supplier = await referenceDataService.GetSupplierAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost("suppliers")]
    [ProducesResponseType<SupplierAdminDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierAdminDto>> CreateSupplier(SupplierWriteRequest request, CancellationToken cancellationToken)
    {
        var supplier = await referenceDataService.CreateSupplierAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
    }

    [HttpPut("suppliers/{id:int}")]
    [ProducesResponseType<SupplierAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierAdminDto>> UpdateSupplier(int id, SupplierWriteRequest request, CancellationToken cancellationToken)
    {
        var supplier = await referenceDataService.UpdateSupplierAsync(id, request, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpDelete("suppliers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSupplier(int id, CancellationToken cancellationToken) =>
        await referenceDataService.DeleteSupplierAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("delivery-methods")]
    [ProducesResponseType<IReadOnlyList<DeliveryMethodAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeliveryMethodAdminDto>>> GetDeliveryMethods(CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetDeliveryMethodsAsync(cancellationToken));

    [HttpGet("delivery-methods/{id:int}")]
    [ProducesResponseType<DeliveryMethodAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryMethodAdminDto>> GetDeliveryMethod(int id, CancellationToken cancellationToken)
    {
        var method = await referenceDataService.GetDeliveryMethodAsync(id, cancellationToken);
        return method is null ? NotFound() : Ok(method);
    }

    [HttpPost("delivery-methods")]
    [ProducesResponseType<DeliveryMethodAdminDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DeliveryMethodAdminDto>> CreateDeliveryMethod(DeliveryMethodWriteRequest request, CancellationToken cancellationToken)
    {
        var method = await referenceDataService.CreateDeliveryMethodAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDeliveryMethod), new { id = method.Id }, method);
    }

    [HttpPut("delivery-methods/{id:int}")]
    [ProducesResponseType<DeliveryMethodAdminDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeliveryMethodAdminDto>> UpdateDeliveryMethod(int id, DeliveryMethodWriteRequest request, CancellationToken cancellationToken)
    {
        var method = await referenceDataService.UpdateDeliveryMethodAsync(id, request, cancellationToken);
        return method is null ? NotFound() : Ok(method);
    }

    [HttpDelete("delivery-methods/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDeliveryMethod(int id, CancellationToken cancellationToken) =>
        await referenceDataService.DeleteDeliveryMethodAsync(id, cancellationToken) ? NoContent() : NotFound();
}