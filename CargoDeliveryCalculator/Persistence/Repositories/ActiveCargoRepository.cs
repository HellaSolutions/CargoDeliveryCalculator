using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NodaTime;

namespace CargoDeliveryCalculator.Persistence.Repositories;

public interface IActiveCargoRepository
{
    
    Task<Tuple<ActiveCargoDao, CargoDao, DestinationDao, ShippingDao>?> GetActiveCargoAsync(string userCargoId);
    Task<ActiveCargoDao> CreateActiveCargoAsync(ActiveCargoDao activeCargoDao);
    Task<List<ActiveCargoDao>> GetActiveCargosByPriceIdAsync(PriceDao priceDao, int month);
    Task LockForDischargeAsync(string userCargoId);
    Task<EstimateDao?> GetLastEstimate(string userCargoId);
    Task<EstimateDao?> GetEstimate(string userCargoId, Instant instant);
    Task<EstimateDao?> GetEstimate(string userCargoId, int version);
    Task<List<EstimateDao>> GetEstimate(string userCargoId, Instant fromInstant, Instant toInstant);
}

public class ActiveCargoRepository(AppDbContext context) : IActiveCargoRepository
{
    public async Task<Tuple<ActiveCargoDao, CargoDao, DestinationDao, ShippingDao>?> GetActiveCargoAsync(
        string userCargoId)
    {
       var query = from ac in context.ActiveCargos
            join c in context.Cargos on ac.CargoId equals c.Id
            join d in context.Destinations on ac.DestinationId equals d.Id
            join s in context.Shippings on ac.ShippingId equals s.Id
            where c.CargoId == userCargoId
            select new Tuple<ActiveCargoDao, CargoDao, DestinationDao, ShippingDao>(ac, c, d, s);
       return await query.FirstOrDefaultAsync();
    }
    
    public async Task<EstimateDao?> GetLastEstimate(
        string userCargoId)
    {
        var query = from ac in context.ActiveCargos
            join c in context.Cargos on ac.CargoId equals c.Id
            join e in context.Estimates on new { ActiveCargoId = ac.Id, Version = ac.LastEstimateVersion }
                equals new { e.ActiveCargoId, e.Version }
            where c.CargoId == userCargoId
            select e;
                
        return await query.SingleOrDefaultAsync();
    }
    
    public async Task<List<EstimateDao>> GetEstimate(string userCargoId, Instant fromInstant, Instant toInstant)
    {
        var query = from ac in context.ActiveCargos
            join c in context.Cargos on ac.CargoId equals c.Id
            join e in context.Estimates on ac.Id equals e.ActiveCargoId
            where c.CargoId == userCargoId
                  && e.CreationInstant >= fromInstant
                  && e.CreationInstant < toInstant
            orderby e.Version
            select e;

        return await query.ToListAsync();

    }
    
    // The version current at the given instant: the latest one created at or before it
    public async Task<EstimateDao?> GetEstimate(string userCargoId, Instant instant)
    {
        var query = from ac in context.ActiveCargos
            join c in context.Cargos on ac.CargoId equals c.Id
            join e in context.Estimates on ac.Id equals e.ActiveCargoId
            where c.CargoId == userCargoId
                  && e.CreationInstant <= instant
            orderby e.Version descending
            select e;

        return await query.FirstOrDefaultAsync();
    }

    public async Task<EstimateDao?> GetEstimate(string userCargoId, int version)
    {
        var query = from ac in context.ActiveCargos
            join c in context.Cargos on ac.CargoId equals c.Id
            join e in context.Estimates on new { ActiveCargoId = ac.Id, Version = version }
                equals new { e.ActiveCargoId, e.Version }
            where c.CargoId == userCargoId
            select e;
                
        return await query.SingleOrDefaultAsync();
    }
    
    public async Task<ActiveCargoDao> CreateActiveCargoAsync(ActiveCargoDao activeCargoDao)
    {
        var c = await context.AddAsync(activeCargoDao);
        await context.SaveChangesAsync();
        return c.Entity;
    }

    public async Task<List<ActiveCargoDao>> GetActiveCargosByPriceIdAsync(PriceDao priceDao, int month)
    {
        // Pessimistic Lock the rows first: a concurrent price change touching the same cargos waits here until we commit.                                                                                                                                                                                                       
        await context.Database.ExecuteSqlAsync($"""
                                                SELECT 1 FROM "ActiveCargos"                                                                                                                                                                                                                                                                             
                                                WHERE ("LoadPricesId" = {priceDao.Id} AND "LoadMonth" = {month})                                                                                                                                                                                                                                         
                                                OR ("DischargePricesId" = {priceDao.Id} AND "DischargeMonth" = {month})                                                                                                                                                                                                                               
                                                FOR UPDATE  
                                                """);  
        
        return await context.ActiveCargos
            .Include(ac => ac.Cargo)
            .Include(ac => ac.Destination)
            .Include(ac => ac.Shipping)
            .Where(ac => (ac.LoadPricesId == priceDao.Id && ac.LoadMonth == month) || (ac.DischargePricesId == priceDao.Id && ac.DischargeMonth == month))
            .ToListAsync();
    }
    
    // Lock order matches the price-change path: price rows first, then the active cargo row.
    public async Task LockForDischargeAsync(string userCargoId)
    {
        await context.Database.ExecuteSqlAsync($"""
                                                SELECT 1 FROM "Prices" p
                                                JOIN "ActiveCargos" ac ON p."Id" IN (ac."LoadPricesId", ac."DischargePricesId")
                                                JOIN "Cargos" c ON c."Id" = ac."CargoId"
                                                WHERE c."CargoId" = {userCargoId}
                                                FOR SHARE OF p
                                                """);

        await context.Database.ExecuteSqlAsync($"""
                                                SELECT 1 FROM "ActiveCargos" ac
                                                JOIN "Cargos" c ON c."Id" = ac."CargoId"
                                                WHERE c."CargoId" = {userCargoId}
                                                FOR UPDATE OF ac
                                                """);
    }
}