using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RetireMe.Core.Taxes
{
    public static class TaxPolicyLoader
    {
        private const string TaxDataFolder = "TaxData";

        public static TaxPolicy LoadForYear(int year)
        {
            EnsureTaxDataFolderExists();

            // Try exact match first
            string exactPath = Path.Combine(TaxDataFolder, $"Tax{year}.json");
            if (File.Exists(exactPath))
                return LoadFromFile(exactPath);

            // Otherwise fall back to closest prior year
            var availableYears = Directory.GetFiles(TaxDataFolder, "Tax*.json")
                .Select(f => ExtractYearFromFilename(f))
                .Where(y => y <= year)
                .OrderByDescending(y => y)
                .ToList();

            if (availableYears.Count == 0)
                throw new Exception($"No tax policy files found for year {year} or earlier.");

            int fallbackYear = availableYears.First();
            string fallbackPath = Path.Combine(TaxDataFolder, $"Tax{fallbackYear}.json");

            return LoadFromFile(fallbackPath);
        }

        private static TaxPolicy LoadFromFile(string path)
        {
            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true // ensures IRMAA and future fields deserialize cleanly
            };

            var policy = JsonSerializer.Deserialize<TaxPolicy>(json, options);

            if (policy == null)
                throw new Exception($"Failed to deserialize tax policy file: {path}");

            return policy;
        }

        private static int ExtractYearFromFilename(string filePath)
        {
            string file = Path.GetFileNameWithoutExtension(filePath); // "Tax2025"
            string yearPart = file.Substring(3); // "2025"
            return int.Parse(yearPart);
        }

        private static void EnsureTaxDataFolderExists()
        {
            if (!Directory.Exists(TaxDataFolder))
                Directory.CreateDirectory(TaxDataFolder);
        }
    }
}

