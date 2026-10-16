using CargoDeliveryCalculator.Persistence.Model;

namespace CargoDeliveryCalculator.Persistence.Repositories;

public interface IShippingRepository
{
    public Task<ShippingDao> CreateShippingAsync(ShippingDao shipping);
    Task<ShippingDao?> GetShippingByIdAsync(int id);
}

public class ShippingRepository(AppDbContext context) : IShippingRepository
{
    public async Task<ShippingDao> CreateShippingAsync(ShippingDao shipping)
    {
        var c = await context.AddAsync(shipping);
        await context.SaveChangesAsync();
        return c.Entity;
    }
    
    public async Task<ShippingDao?> GetShippingByIdAsync(int id)
    {
        return await context.Shippings.FindAsync(id);
    }
}