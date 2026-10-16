using CargoDeliveryCalculator.Persistence.Model;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;

public interface IMarketDataRepository
{
    Task<(PriceDao Price, bool Created, bool Changed)> SetIndexPriceAsync(decimal amount, int year, int month,
        Index index);

    Task<PriceDao?> GetIndexPriceAsync(Index index, int year);
    Task LockPricesForShareAsync(Index loadIndex, int loadYear, Index saleIndex, int saleYear);
}

public class MarketDataRepository(AppDbContext context) : IMarketDataRepository
{
    public async Task<(PriceDao Price, bool Created, bool Changed)> SetIndexPriceAsync(decimal amount, int year,
        int month, Index index)
    {
        var created = false;
        var price = await _GetIndexPriceByYear(index, year);
        if (price != null)
        {
            if (price.GetPrice(month) == amount)
            {
                return (price, false, false);
            }

            price.SetPrice(month, amount);
        }
        else
        {
            price = new PriceDao(index, year);
            price.SetPrice(month, amount);
            await context.Prices.AddAsync(price);
            created = true;
        }

        await context.SaveChangesAsync();
        return (price, created, true);
    }

    public async Task<PriceDao?> GetIndexPriceAsync(Index index, int year)
    {
        return (await _GetIndexPriceByYear(index, year));
    }

    public async Task LockPricesForShareAsync(Index loadIndex, int loadYear, Index saleIndex, int saleYear)
    {
        await context.Database.ExecuteSqlAsync(
            $"""                                                                                                                                                                                                                                                              
             SELECT 1 FROM "Prices"                                                                                                                                                                                                                                                                               
             WHERE ("Index" = {loadIndex.ToString()} AND "Year" = {loadYear})                                                                                                                                                                                                                                     
                OR ("Index" = {saleIndex.ToString()} AND "Year" = {saleYear})                                                                                                                                                                                                                                     
             FOR SHARE                                                                                                                                                                                                                                                                                            
             """);
    }

    private async Task<PriceDao?> _GetIndexPriceByYear(Index index, int year)
    {
        return await (from p in context.Prices
            where p.Year == year && p.Index == index
            select p).FirstOrDefaultAsync();
    }
}