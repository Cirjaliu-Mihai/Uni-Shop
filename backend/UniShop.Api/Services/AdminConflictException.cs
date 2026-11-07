namespace UniShop.Api.Services;

public sealed class AdminConflictException(string message) : Exception(message);

public sealed class ShoppingConflictException(string message) : Exception(message);