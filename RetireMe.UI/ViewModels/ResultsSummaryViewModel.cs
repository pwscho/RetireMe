using ClosedXML.Excel;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using RetireMe.Core;
using RetireMe.Core.Reporting;
using RetireMe.UI.Reporting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace RetireMe.UI.ViewModels
{
    public class ResultsSummaryViewModel : ViewModelBase
    {
        // ------------------------------------------------------------
        // Raw Data
        // ------------------------------------------------------------
        public Scenario Scenario { get; }

        public List<ResultRow> FixedResults { get; }
        public List<ResultRow> HistoricalResults { get; }
        public List<ResultRow> MonteCarloResults { get; }


        // ------------------------------------------------------------
        // Bindable Lists (for DataGrids)
        // ------------------------------------------------------------
        public List<ResultRow> ListOfFixedResults => FixedResults;
        public List<ResultRow> ListOfHistoricalResults => HistoricalResults;
        public List<ResultRow> ListOfMonteCarloResults => MonteCarloResults;

        // ------------------------------------------------------------
        // Histogram (LiveCharts)
        // ------------------------------------------------------------
        public SeriesCollection MonteCarloSeries { get; set; }
        public List<string> MonteCarloLabels { get; set; }

        // ------------------------------------------------------------
        // Trajectory Percentile Bands
        // ------------------------------------------------------------
        public SeriesCollection PercentileSeries { get; set; }
        public List<string> TrajectoryYears { get; set; }
        public Func<double, string> CurrencyFormatter { get; set; }

        public List<HistogramBin> MonteCarloEndingBalanceHistogram { get; }
        // ------------------------------------------------------------
        // Income Summary graph
        // ------------------------------------------------------------
        public SeriesCollection StackedIncomeSeries { get; set; }
        public List<string> StackedIncomeLabels { get; set; }
        // ------------------------------------------------------------
        // Spending Summary graph
        // ------------------------------------------------------------
        public SeriesCollection StackedSpendingSeries { get; set; }
        public List<string> StackedSpendingLabels { get; set; }
        // ------------------------------------------------------------
        // Historical Chart
        // ------------------------------------------------------------
        public SeriesCollection HistoricalEndingSeries { get; set; }
        public List<string> HistoricalEndingLabels { get; set; }
        public Func<double, string> DollarFormatter { get; set; }

        // ------------------------------------------------------------
        // Summary Statistics
        // ------------------------------------------------------------
        public decimal FixedSuccessRate { get; }
        public decimal SequenceSuccessRate { get; }
        public decimal RandomSuccessRate { get; }

        public decimal FixedAvgBalance { get; }
        public decimal SequenceAvgBalance { get; }
        public decimal RandomAvgBalance { get; }

        public decimal FixedMedBalance { get; }
        public decimal SequenceMedBalance { get; }
        public decimal RandomMedBalance { get; }

        public decimal Sequence10thPercentile { get; }
        public decimal Sequence90thPercentile { get; }

        public decimal Random10thPercentile { get; }
        public decimal Random90thPercentile { get; }

        public decimal FixedTaxesPaid { get; }
        public decimal SequenceTaxesPaid { get; }
        public decimal RandomTaxesPaid { get; }

        public ICommand ExportToExcelCommand { get; }


        // ------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------
        public ResultsSummaryViewModel(ReportData data)
        {
            Scenario = data.Scenario;

            FixedResults = data.FixedResults;
            HistoricalResults = data.HistoricalResults;
            MonteCarloResults = data.MonteCarloResults;

            // Summary Stats
            FixedSuccessRate = ComputeSuccessRate(FixedResults);
            SequenceSuccessRate = ComputeSuccessRate(HistoricalResults);
            RandomSuccessRate = ComputeSuccessRate(MonteCarloResults);

            FixedAvgBalance = ComputeAverageEndingBalance(FixedResults);
            SequenceAvgBalance = ComputeAverageEndingBalance(HistoricalResults);
            RandomAvgBalance = ComputeAverageEndingBalance(MonteCarloResults);

            FixedMedBalance = ComputeMedianEndingBalance(FixedResults);
            SequenceMedBalance = ComputeMedianEndingBalance(HistoricalResults);
            RandomMedBalance = ComputeMedianEndingBalance(MonteCarloResults);

            Sequence10thPercentile = ComputePercentile(HistoricalResults, 0.10m);
            Sequence90thPercentile = ComputePercentile(HistoricalResults, 0.90m);

            Random10thPercentile = ComputePercentile(MonteCarloResults, 0.10m);
            Random90thPercentile = ComputePercentile(MonteCarloResults, 0.90m);

            FixedTaxesPaid = ComputeAverageTaxes(FixedResults);
            SequenceTaxesPaid = ComputeAverageTaxes(HistoricalResults);
            RandomTaxesPaid = ComputeAverageTaxes(MonteCarloResults);

            // ------------------------------------------------------------
            // Monte Carlo Histogram (with Winsorization)
            // ------------------------------------------------------------
            var endings = MonteCarloResults
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Last().EndingBalance)
                .ToList();

            var cleanedEndings = Winsorize(endings, 0.99m);

            var histogramBuilder = new EndingBalanceHistogramBuilder();
            MonteCarloEndingBalanceHistogram = histogramBuilder.Build(cleanedEndings, binCount: 50);

            BuildLiveChartsHistogram(MonteCarloEndingBalanceHistogram);

            // ------------------------------------------------------------
            // Percentile Trajectory Chart
            // ------------------------------------------------------------
            BuildPercentileTrajectoryChart(MonteCarloResults);

            // ------------------------------------------------------------
            // Stacked Income Chart
            // ------------------------------------------------------------
            BuildStackedIncomeChart();
            // ------------------------------------------------------------
            // Stacked Income Chart
            // ------------------------------------------------------------
            BuildStackedSpendingChart();
            // ------------------------------------------------------------
            // Historical Bar Chart
            // ------------------------------------------------------------
            BuildHistoricalEndingBalanceChart();

            // Excel Export Command
            ExportToExcelCommand = new RelayCommand(ExportToExcel);

        }

        // ------------------------------------------------------------
        // Helper Methods
        // ------------------------------------------------------------
        private decimal ComputeSuccessRate(List<ResultRow> list)
        {
            if (list == null || list.Count == 0)
                return 0;

            var sims = list.GroupBy(r => r.SimNo);

            int total = sims.Count();
            int survived = sims.Count(sim => sim.Last().EndingBalance > 0);

            return total == 0 ? 0 : (decimal)survived / total;
        }

        private decimal ComputeAverageEndingBalance(List<ResultRow> list)
        {
            if (list == null || list.Count == 0)
                return 0;

            return list
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Last().EndingBalance)
                .Average();
        }

        private List<decimal> Winsorize(List<decimal> values, decimal percentile)
        {
            if (values == null || values.Count == 0)
                return new List<decimal>();

            var sorted = values.OrderBy(v => v).ToList();
            int index = (int)(percentile * (sorted.Count - 1));
            decimal cap = sorted[index];

            return values.Select(v => Math.Min(v, cap)).ToList();
        }

        private decimal ComputeMedianEndingBalance(List<ResultRow> list)
        {
            if (list == null || list.Count == 0)
                return 0;

            var endings = list
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Last().EndingBalance)
                .OrderBy(x => x)
                .ToList();

            int count = endings.Count;
            if (count == 0)
                return 0;

            if (count % 2 == 1)
                return endings[count / 2];

            return (endings[count / 2] + endings[count / 2 - 1]) / 2m;
        }

        private decimal ComputePercentile(List<ResultRow> list, decimal percentile)
        {
            if (list == null || list.Count == 0)
                return 0;

            var endings = list
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Last().EndingBalance)
                .OrderBy(x => x)
                .ToList();

            if (endings.Count == 0)
                return 0;

            int index = (int)Math.Floor(percentile * (endings.Count - 1));
            return endings[index];
        }

        private decimal ComputeAverageTaxes(List<ResultRow> list)
        {
            if (list == null || list.Count == 0)
                return 0;

            return list
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Sum(x => x.TaxesPaid))
                .Average();
        }
        // Charts for Monte Carlo Results
        // ------------------------------------------------------------
        // Histogram Builder (LiveCharts)
        // ------------------------------------------------------------
        private void BuildLiveChartsHistogram(List<HistogramBin> bins)
        {
            if (bins == null || bins.Count == 0)
            {
                MonteCarloSeries = new SeriesCollection();
                MonteCarloLabels = new List<string>();
                return;
            }

            var counts = bins.Select(b => (double)b.Count).ToArray();

            MonteCarloLabels = bins
                .Select(b => b.BinStart.ToString("C0"))
                .ToList();

            MonteCarloSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Frequency",
                    Values = new ChartValues<double>(counts),
                    Fill = System.Windows.Media.Brushes.SteelBlue,
                    StrokeThickness = 0,
                    MaxColumnWidth = 20
                    
                }
            };
        }

        // ------------------------------------------------------------
        // Percentile Trajectory Chart
        // ------------------------------------------------------------
        private void BuildPercentileTrajectoryChart(List<ResultRow> monteCarloResults)
        {
            var sims = monteCarloResults
                .GroupBy(r => r.SimNo)
                .OrderBy(g => g.Key)
                .ToList();

            int yearCount = sims.First().Count();

            var matrix = sims
                .Select(sim => sim.OrderBy(r => r.Year)
                .Select(r => r.EndingBalance)
                .ToList())
                .ToList();

            var p10 = new List<double>();
            var p50 = new List<double>();
            var p90 = new List<double>();

            for (int year = 0; year < yearCount; year++)
            {
                var values = matrix.Select(row => row[year]).OrderBy(v => v).ToList();
                int count = values.Count;

                p10.Add((double)values[(int)(0.10m * (count - 1))]);
                p50.Add((double)values[(int)(0.50m * (count - 1))]);
                p90.Add((double)values[(int)(0.90m * (count - 1))]);
            }

            TrajectoryYears = sims.First()
                .OrderBy(r => r.Year)
                .Select(r => r.Year.ToString())
                .ToList();

            PercentileSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "10th Percentile",
                    Values = new ChartValues<double>(p10),
                    StrokeThickness = 2,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    LabelPoint = point => point.Y.ToString("C0"),
                    Stroke = System.Windows.Media.Brushes.IndianRed
                },
                new LineSeries
                {
                    Title = "Median (50th)",
                    Values = new ChartValues<double>(p50),
                    StrokeThickness = 3,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    LabelPoint = point => point.Y.ToString("C0"),
                    Stroke = System.Windows.Media.Brushes.DarkBlue
                },
                new LineSeries
                {
                    Title = "90th Percentile",
                    Values = new ChartValues<double>(p90),
                    StrokeThickness = 2,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    LabelPoint = point => point.Y.ToString("C0"),
                    Stroke = System.Windows.Media.Brushes.ForestGreen
                }
            };

            CurrencyFormatter = value => value.ToString("C0");
        }

        // Stacked income chart


        private void BuildStackedIncomeChart()
        {
            var grouped = FixedResults
                .GroupBy(r => r.Year)
                .OrderBy(g => g.Key)
                .ToList();

            // X-axis labels
            StackedIncomeLabels = grouped
                .Select(g => g.Key.ToString())
                .ToList();

            // Build sequences
            var income = grouped
                .Select(g => g.Sum(r => (double)r.Income))
                .ToList();

            var investmentReturn = grouped
                .Select(g => g.Sum(r => (double)r.InvestmentReturn))
                .ToList();

            var socialSecurity = grouped
                .Select(g => g.Sum(r => (double)r.SocialSecurityIncome))
                .ToList();


            StackedIncomeSeries = new SeriesCollection
    {
        new StackedAreaSeries
        {
            Title = "Income",
            Values = new ChartValues<double>(income),
            Fill = Brushes.SteelBlue,
            LabelPoint = point => point.Y.ToString("C0"),
            LineSmoothness = 0.1
        },
        new StackedAreaSeries
        {
            Title = "Investment Return",
            Values = new ChartValues<double>(investmentReturn),
            Fill = Brushes.ForestGreen,
            LabelPoint = point => point.Y.ToString("C0"),
            LineSmoothness = 0.1
        },
        new StackedAreaSeries
        {
            Title = "Social Security",
            Values = new ChartValues<double>(socialSecurity),
            Fill = Brushes.Orange,
            LabelPoint = point => point.Y.ToString("C0"),
            LineSmoothness = 0.1
        }
    };

            OnPropertyChanged(nameof(StackedIncomeSeries));
            OnPropertyChanged(nameof(StackedIncomeLabels));
        }


        private void BuildStackedSpendingChart()
        {
            var grouped = FixedResults
                .GroupBy(r => r.Year)
                .OrderBy(g => g.Key)
                .ToList();

            // X-axis labels
            StackedSpendingLabels = grouped
                .Select(g => g.Key.ToString())
                .ToList();

            // Build sequences
            var withdrawals = grouped
                .Select(g => g.Sum(r => (double)r.Withdrawal))
                .ToList();

            var taxesPaid = grouped
                .Select(g => g.Sum(r => (double)(r.TaxesPaid - r.IrmaaPaid)))
                .ToList();

            var irmaaPaid = grouped
                .Select(g => g.Sum(r => (double)r.IrmaaPaid))
                .ToList();

            StackedSpendingSeries = new SeriesCollection
    {
        new StackedAreaSeries
        {
            Title = "Withdrawals",
            Values = new ChartValues<double>(withdrawals),            
            Fill = Brushes.SteelBlue,
            LineSmoothness = 0.1,
            LabelPoint = point => point.Y.ToString("C0"),
            PointGeometry = null
        },
        new StackedAreaSeries
        {
            Title = "Taxes Paid",
            Values = new ChartValues<double>(taxesPaid),
            Fill = Brushes.ForestGreen,
            LineSmoothness = 0.1,
            LabelPoint = point => point.Y.ToString("C0"),
            PointGeometry = null
        },
        new StackedAreaSeries
        {
            Title = "IRMAA Paid",
            Values = new ChartValues<double>(irmaaPaid),
            Fill = Brushes.Orange,
            LineSmoothness = 0.1,
            LabelPoint = point => point.Y.ToString("C0"),
            PointGeometry = null
        }
    };

            OnPropertyChanged(nameof(StackedSpendingSeries));
            OnPropertyChanged(nameof(StackedSpendingLabels));
        }


        public void BuildHistoricalEndingBalanceChart()
        {
            // Currency formatting
            DollarFormatter = value => value.ToString("C0");

            // 1. Extract the final row of each simulation
            var finalRows = HistoricalResults
                .GroupBy(r => r.SimNo)
                .Select(g => g.OrderByDescending(r => r.Year).First())
                .OrderBy(r => r.SimNo)
                .ToList();

            int simCount = finalRows.Count;

            // 2. Build X-axis labels starting at 1928
            int firstYear = 1928;

            HistoricalEndingLabels = Enumerable
                .Range(firstYear, simCount)
                .Select(y => y.ToString())
                .ToList();

            // 3. Extract ending balances
            var endingBalances = finalRows
                .Select(r => (double)r.EndingBalance)
                .ToList();

            // 4. Build the bar series
            HistoricalEndingSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Ending Balance",
            Values = new ChartValues<double>(endingBalances),
            Fill = new SolidColorBrush(Color.FromArgb(200, 70, 130, 180)),
            Stroke = Brushes.SteelBlue,
            StrokeThickness = 2,
            LabelPoint = point => point.Y.ToString("C0")
        }
    };

            OnPropertyChanged(nameof(HistoricalEndingSeries));
            OnPropertyChanged(nameof(HistoricalEndingLabels));
            OnPropertyChanged(nameof(DollarFormatter));
        }

        private void ExportToExcel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "SimulationResults.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            using (var wb = new XLWorkbook())
            {
                // ----------------------------------------------------
                // 1. Scenario Sheet
                // ----------------------------------------------------
                var wsScenario = wb.Worksheets.Add("Scenario");
                wsScenario.Cell(1, 1).Value = "Scenario Details";
                wsScenario.Cell(1, 1).Style.Font.Bold = true;

                int row = 3;
                wsScenario.Cell(row++, 1).Value = $"Base Year: {Scenario.BaseYear}";
                wsScenario.Cell(row++, 1).Value = $"Years: {Scenario.YearsToCalculate}";
                wsScenario.Cell(row++, 1).Value = $"Inflation Rate: {Scenario.InflationRate:P2}";
                wsScenario.Cell(row++, 1).Value = $"Withdrawal Rate: {Scenario.AnnualSpending:C2}";
                wsScenario.Columns().AdjustToContents();

                // ----------------------------------------------------
                // 2. Fixed Results Sheet
                // ----------------------------------------------------
                WriteResultRowsSheet(wb, "Fixed Results", FixedResults);

                // ----------------------------------------------------
                // 3. Historical Results Sheet
                // ----------------------------------------------------
                WriteResultRowsSheet(wb, "Historical Results", HistoricalResults);

                // ----------------------------------------------------
                // 4. Monte Carlo Results Sheet
                // ----------------------------------------------------
                WriteResultRowsSheet(wb, "Monte Carlo Results", MonteCarloResults);

                wb.SaveAs(dialog.FileName);
            }
        }

            private void WriteResultRowsSheet(XLWorkbook wb, string sheetName, List<ResultRow> rows)
        {
            var ws = wb.Worksheets.Add(sheetName);

            // Header row
            ws.Cell(1, 1).Value = "SimNo";
            ws.Cell(1, 2).Value = "Year";
            ws.Cell(1, 3).Value = "Age";
            ws.Cell(1, 4).Value = "SpouseAge";
            ws.Cell(1, 5).Value = "Iteration";
            ws.Cell(1, 6).Value = "StartingBalance";
            ws.Cell(1, 7).Value = "EndingBalance";
            ws.Cell(1, 8).Value = "Withdrawal";
            ws.Cell(1, 9).Value = "SocialSecurityIncome";
            ws.Cell(1, 10).Value = "Income";
            ws.Cell(1, 11).Value = "InvestmentReturn";
            ws.Cell(1, 12).Value = "Magi";
            ws.Cell(1, 13).Value = "TaxableIncome";
            ws.Cell(1, 14).Value = "OrdinaryIncome";
            ws.Cell(1, 15).Value = "CapitalGains";
            ws.Cell(1, 16).Value = "TaxesDue";
            ws.Cell(1, 17).Value = "TaxesPaid";
            ws.Cell(1, 18).Value = "IrmaaPaid";
            ws.Cell(1, 19).Value = "Rmds";
            ws.Cell(1, 20).Value = "Conversions";

            int row = 2;

            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.SimNo;
                ws.Cell(row, 2).Value = r.Year;
                ws.Cell(row, 3).Value = r.Age;
                ws.Cell(row, 4).Value = r.SpouseAge;
                ws.Cell(row, 5).Value = r.Iteration;
                ws.Cell(row, 6).Value = r.StartingBalance;
                ws.Cell(row, 7).Value = r.EndingBalance;
                ws.Cell(row, 8).Value = r.Withdrawal;
                ws.Cell(row, 9).Value = r.SocialSecurityIncome;
                ws.Cell(row, 10).Value = r.Income;
                ws.Cell(row, 11).Value = r.InvestmentReturn;
                ws.Cell(row, 12).Value = r.Magi;
                ws.Cell(row, 13).Value = r.TaxableIncome;
                ws.Cell(row, 14).Value = r.OrdinaryIncome;
                ws.Cell(row, 15).Value = r.CapitalGains;
                ws.Cell(row, 16).Value = r.TaxesDue;
                ws.Cell(row, 17).Value = r.TaxesPaid;
                ws.Cell(row, 18).Value = r.IrmaaPaid;
                ws.Cell(row, 19).Value = r.Rmds;
                ws.Cell(row, 20).Value = r.Conversions;

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

    }

}






