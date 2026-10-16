using CargoDeliveryCalculator.Persistence.Model;

namespace CargoDeliveryCalculator.Persistence.Repositories;

public interface IEstimateRepository
{
    public Task<EstimateDao> CreateEstimateAsync(EstimateDao estimate);
    public Task<EstimateDao?> GetEstimateByIdAsync(int id);
}

public class EstimateRepository(AppDbContext context) : IEstimateRepository
{
    public async Task<EstimateDao> CreateEstimateAsync(EstimateDao estimate)
    {
        var c = await context.AddAsync(estimate);
        await context.SaveChangesAsync();
        return c.Entity;
    }
    
    public async Task<EstimateDao?> GetEstimateByIdAsync(int id)
    {
        return await context.Estimates.FindAsync(id);
    }
}