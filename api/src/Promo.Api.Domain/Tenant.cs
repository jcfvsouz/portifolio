namespace Promo.Api.Domain;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Tenant()
    {
    }

    public Tenant(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
