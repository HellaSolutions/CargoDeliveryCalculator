using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CargoDeliveryCalculator.Persistence.Repositories;

public interface ICargoRepository
{
    public Task<CargoDao> CreateCargoAsync(CargoDao cargo);
    public Task<CargoDao?> GetCargoByIdAsync(int id);
    
    public Task<CargoDao?> GetCargoByCargoIdAsync(string cargoId);
}

public class CargoRepository(AppDbContext context) : ICargoRepository
{
    public async Task<CargoDao> CreateCargoAsync(CargoDao cargo)
    {
        try
        {
            var c = await context.AddAsync(cargo);
            await context.SaveChangesAsync();
            return c.Entity;
        }
        catch (DbUpdateException e)
        {
            if (e.InnerException!.Message.StartsWith("23505: duplicate key value violates unique constraint \"IX_Cargos_CargoId\""))
            {                                
                throw new ArgumentException($"Cannot activate the same cargo twice. CargoId: {cargo.CargoId}");
            }

            throw;
        }
    }

    public async Task<CargoDao?> GetCargoByIdAsync(int id)
    {
        return await context.Cargos.FindAsync(id);
    }

    public async Task<CargoDao?> GetCargoByCargoIdAsync(string cargoId)
    {
        return await context.Cargos
            .Where(c => c.CargoId == cargoId)
            .FirstAsync();
    }
}