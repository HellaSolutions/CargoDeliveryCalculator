using CargoDeliveryCalculator.Persistence.Model;

namespace CargoDeliveryCalculator.Persistence.Repositories;

public interface IDestinationRepository
{
    public Task<DestinationDao> CreateDestinationAsync(DestinationDao destination);
    
    public Task<DestinationDao?> GetDestinationByIdAsync(int id);
}

public class DestinationRepository(AppDbContext context) : IDestinationRepository
{
    public async Task<DestinationDao> CreateDestinationAsync(DestinationDao destination)
    {
        var c = await context.AddAsync(destination);
        await context.SaveChangesAsync();
        return c.Entity;
    }

    public async Task<DestinationDao?> GetDestinationByIdAsync(int id)
    {
        return await context.Destinations.FindAsync(id);
    }
}