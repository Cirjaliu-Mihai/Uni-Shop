namespace UniShop.Api.Services;

public sealed class PaymentProviderUnavailableException(string message) : Exception(message);