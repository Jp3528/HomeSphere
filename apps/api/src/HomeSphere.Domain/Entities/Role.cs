namespace HomeSphere.Domain.Entities;

public class Role
{
    public const string Customer = "CUSTOMER";
    public const string Admin = "ADMIN";
    public const string Warehouse = "WAREHOUSE";
    public const string Manager = "MANAGER";

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
