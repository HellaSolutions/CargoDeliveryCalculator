using CargoDeliveryCalculator.Model;
using CargoDeliveryCalculator.Persistence;
using CargoDeliveryCalculator.Persistence.Model;
using CargoDeliveryCalculator.Persistence.Repositories;
using CargoDeliveryCalculator.Util;
using NodaTime;
using Index = CargoDeliveryCalculator.Model.Index;

namespace CargoDeliveryCalculator.Services;

public interface IMarketDataService
{
    Task<PriceDao> GetIndexPriceAsync(string index, int year);
    Task<PriceDao> GetIndexPriceAsync(Index index, int year);
    Task<(PriceDao Price, bool Created, bool Changed)> SetIndexPriceAsync(decimal amount, int year, int month,
        Index index);
    Task LockPricesForShareAsync(Index loadIndex, int loadYear, Index saleIndex, int saleYear);
}

public class MarketDataService(IMarketDataRepository marketDataRepository)
    : IMarketDataService
{

    public async Task<PriceDao> GetIndexPriceAsync(string index, int year)
    {
        return !Enum.TryParse<Index>(index, true, out var tradeIndex) ? 
            throw new ArgumentException($"Invalid index {index}") : 
            await GetIndexPriceAsync(tradeIndex, year);
    }

    public async Task<PriceDao> GetIndexPriceAsync(Index index, int year)
    {
        return await marketDataRepository.GetIndexPriceAsync(index, year)??
               throw IndexUnavailableException.ForYear(index, year);
    }

    public async Task<(PriceDao Price, bool Created, bool Changed)> SetIndexPriceAsync(decimal amount, int year, int month, Index index)
    {
        return await marketDataRepository.SetIndexPriceAsync(amount, year, month, index);
    }

    public Task LockPricesForShareAsync(Index loadIndex, int loadYear, Index saleIndex, int saleYear) => 
        marketDataRepository.LockPricesForShareAsync(loadIndex, loadYear, saleIndex, saleYear); 
    
}