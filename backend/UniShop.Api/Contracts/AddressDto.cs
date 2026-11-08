namespace UniShop.Api.Contracts;

public sealed record AddressDto(
    int Id,
    string Label,
    string FullName,
    string Phone,
    string Street,
    string City,
    string County,
    string PostalCode,
    string Country,
    bool IsDefault);