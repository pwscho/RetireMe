using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RetireMe.Core
{
    public static class RmdPolicyLoader
    {
        private const string FolderName = "RmdData";

        public static RmdPolicy LoadForYear(int year)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folderPath = Path.Combine(baseDir, FolderName);

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException(
                    $"RMD policy folder not found: {folderPath}");

            // Look for files like RMD2025.json, RMD2026.json, etc.
            var files = Directory.GetFiles(folderPath, "RMD*.json");

            if (files.Length == 0)
                throw new FileNotFoundException(
                    $"No RMD policy files found in {folderPath}");

            // Extract available years
            var availableYears = files
                .Select(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f); // RMD2025
                    string yearPart = name.Substring(3);               // 2025
                    return int.TryParse(yearPart, out int y) ? y : -1;
                })
                .Where(y => y > 0)
                .OrderBy(y => y)
                .ToList();

            if (availableYears.Count == 0)
                throw new Exception("No valid RMD policy years found.");

            // Find the best match: exact year or closest prior year
            int selectedYear = availableYears
                .Where(y => y <= year)
                .DefaultIfEmpty(availableYears.Max())
                .Max();

            string selectedFile = Path.Combine(folderPath, $"RMD{selectedYear}.json");

            if (!File.Exists(selectedFile))
                throw new FileNotFoundException(
                    $"RMD policy file not found: {selectedFile}");

            string json = File.ReadAllText(selectedFile);

            var policy = JsonSerializer.Deserialize<RmdPolicy>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (policy == null)
                throw new Exception($"Failed to deserialize RMD policy from {selectedFile}");

            return policy;
        }
    }
}

