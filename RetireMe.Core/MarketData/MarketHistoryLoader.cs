using System.Text.Json;

namespace RetireMe.Core.MarketData
{
    public static class MarketHistoryLoader
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "MarketData", "MarketData.json");

        public static MarketHistory Load()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"Market data file not found: {FilePath}");

            var json = File.ReadAllText(FilePath);

            return JsonSerializer.Deserialize<MarketHistory>(json)
                   ?? new MarketHistory();
        }
    }
}

