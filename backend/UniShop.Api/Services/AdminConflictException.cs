namespace UniShop.Api.Services;

public sealed class AdminConflictException(string message) : Exception(message);