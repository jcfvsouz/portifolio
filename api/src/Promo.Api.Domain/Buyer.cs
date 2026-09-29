namespace Promo.Api.Domain;

public class Buyer
{
    public Guid Id { get; private set; }
    public Guid BuyerGroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Buyer()
    {
    }

    public Buyer(Guid buyerGroupId, string name, string email)
    {
        Id = Guid.NewGuid();
        BuyerGroupId = buyerGroupId;
        Name = name;
        Email = email;
    }
}
