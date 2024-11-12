using Microsoft.AspNetCore.Identity;

namespace Mahfoud.Identity.Entities;

public partial class User: IdentityUser<long>
{
    public User()
    {
    }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? Image { get; set; }
}
