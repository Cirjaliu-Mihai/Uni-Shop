using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record UpdateAdminUserRoleRequest
{
    [Required, RegularExpression("^(Admin|Client)$")]
    public required string Role { get; init; }
}