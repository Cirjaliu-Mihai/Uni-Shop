namespace UniShop.Api.Contracts;

public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal Total, bool HasStockWarnings);