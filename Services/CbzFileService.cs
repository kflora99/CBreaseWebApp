using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Brease.Core.Models;

namespace CBreaseWebApp1.Services
{
    public class CbzFileService
    {
        public string AppendCrossSection(string originalText, Section newSection)
        {
            return AppendCrossSectionToCbzText(originalText, newSection);
        }

        public string ReplaceAllCrossSections(string originalText, IEnumerable<Section> sections)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                throw new InvalidOperationException("CBZ text is empty.");

            const string sectionMarker = "#CROSS SECTION DATA#";
            int crossSectionStart = originalText.IndexOf(sectionMarker, StringComparison.OrdinalIgnoreCase);

            if (crossSectionStart < 0)
                throw new InvalidOperationException("Could not find #CROSS SECTION DATA# section in CBZ text.");

            int searchStart = crossSectionStart + sectionMarker.Length;

            int chartIndex = originalText.IndexOf("#CHART DATA#", searchStart, StringComparison.OrdinalIgnoreCase);
            int hydraulicsIndex = originalText.IndexOf("#HYDRAULICS DATA#", searchStart, StringComparison.OrdinalIgnoreCase);
            int nextKnownSectionIndex = -1;

            if (chartIndex >= 0 && hydraulicsIndex >= 0)
                nextKnownSectionIndex = Math.Min(chartIndex, hydraulicsIndex);
            else if (chartIndex >= 0)
                nextKnownSectionIndex = chartIndex;
            else if (hydraulicsIndex >= 0)
                nextKnownSectionIndex = hydraulicsIndex;

            int crossSectionEnd = nextKnownSectionIndex >= 0 ? nextKnownSectionIndex : originalText.Length;

            string before = originalText.Substring(0, crossSectionStart);
            string after = originalText.Substring(crossSectionEnd);

            var sectionList = sections?.ToList() ?? new List<Section>();

            var rebuiltCrossSectionText = BuildCrossSectionSection(sectionList);

            return before + rebuiltCrossSectionText + after;
        }

        private static string FormatCbzBool(bool value) => value ? "True" : "False";

        private static string EscapeCbzDescription(string? text)
        {
            return (text ?? string.Empty).Replace("\"", "\"\"");
        }

        private static string FormatCbzValue(object? value)
        {
            if (value is null)
                return string.Empty;

            return value switch
            {
                double d => d.ToString("0.00", CultureInfo.InvariantCulture),
                float f => f.ToString("0.00", CultureInfo.InvariantCulture),
                decimal m => m.ToString("0.00", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private static string BuildCrossSectionSection(IEnumerable<Section> sections)
        {
            var list = sections?.ToList() ?? new List<Section>();
            var sb = new StringBuilder();

            sb.AppendLine("#CROSS SECTION DATA#");
            sb.AppendLine($"Number of Cross Section Items={list.Count}");
            sb.AppendLine();

            foreach (var xs in list)
            {
                sb.Append(BuildCrossSectionBlock(xs));
            }

            return sb.ToString();
        }

        private static string BuildCrossSectionBlock(Section xs)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Cross Section Date={xs.Date}");
            sb.AppendLine($"Section Type={xs.Type}");
            sb.AppendLine($"Comments={xs.Comments}");
            sb.AppendLine($"Collector={xs.Collector}");
            sb.AppendLine($"Vertical Offset={FormatCbzValue(xs.VertOffset)}");
            sb.AppendLine($"Vertical Adjustment={FormatCbzValue(xs.VertAdjustment)}");
            sb.AppendLine($"Ref Face={FormatCbzBool(xs.RefFace)}");
            sb.AppendLine($"Ref Constant Elevation={FormatCbzBool(xs.RefConstantVertical)}");

            if (xs.RefConstantVertical)
            {
                sb.AppendLine($"Constant Elevation={FormatCbzValue(xs.RefConstantVerticalElevation)}");
            }

            sb.AppendLine();
            sb.AppendLine($"Number of Points={xs.Data?.Count ?? 0}");
            sb.AppendLine("\tPoint Number, From Item, Horiz. Dist., Vert. Dist., Add Adjust., Description, Station, Elev.");

            if (xs.Data != null)
            {
                foreach (var pt in xs.Data)
                {
                    var desc = $"\"{EscapeCbzDescription(pt.Description)}\"";

                    sb.AppendLine(
                        "\t" +
                        $"{pt.Point}," +
                        $"{FormatCbzValue(pt.FromItem)}," +
                        $"{FormatCbzValue(pt.HDist)}," +
                        $"{FormatCbzValue(pt.VDist)}," +
                        $"{FormatCbzBool(pt.VAdjustment)}," +
                        $"{desc}," +
                        $"{FormatCbzValue(pt.Station)}," +
                        $"{pt.ElevationString}," +
                        $"{FormatCbzValue(pt.FromItemNumber)}");
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static string AppendCrossSectionToCbzText(string originalText, Section newSection)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                throw new InvalidOperationException("CBZ text is empty.");

            const string sectionMarker = "#CROSS SECTION DATA#";
            int crossSectionStart = originalText.IndexOf(sectionMarker, StringComparison.OrdinalIgnoreCase);

            if (crossSectionStart < 0)
                throw new InvalidOperationException("Could not find #CROSS SECTION DATA# section in CBZ text.");

            int searchStart = crossSectionStart + sectionMarker.Length;

            int chartIndex = originalText.IndexOf("#CHART DATA#", searchStart, StringComparison.OrdinalIgnoreCase);
            int hydraulicsIndex = originalText.IndexOf("#HYDRAULICS DATA#", searchStart, StringComparison.OrdinalIgnoreCase);
            int nextKnownSectionIndex = -1;

            if (chartIndex >= 0 && hydraulicsIndex >= 0)
                nextKnownSectionIndex = Math.Min(chartIndex, hydraulicsIndex);
            else if (chartIndex >= 0)
                nextKnownSectionIndex = chartIndex;
            else if (hydraulicsIndex >= 0)
                nextKnownSectionIndex = hydraulicsIndex;

            int crossSectionEnd = nextKnownSectionIndex >= 0 ? nextKnownSectionIndex : originalText.Length;

            string before = originalText.Substring(0, crossSectionStart);
            string crossSectionText = originalText.Substring(crossSectionStart, crossSectionEnd - crossSectionStart);
            string after = originalText.Substring(crossSectionEnd);

            var countRegex = new Regex(@"Number of Cross Section Items=(\d+)", RegexOptions.IgnoreCase);
            var countMatch = countRegex.Match(crossSectionText);

            if (!countMatch.Success)
                throw new InvalidOperationException("Could not find Number of Cross Section Items in cross-section section.");

            int currentCount = int.Parse(countMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            int newCount = currentCount + 1;

            crossSectionText = countRegex.Replace(
                crossSectionText,
                $"Number of Cross Section Items={newCount}",
                1);

            string newBlock = BuildCrossSectionBlock(newSection);

            crossSectionText =
                crossSectionText.TrimEnd('\r', '\n') +
                Environment.NewLine +
                Environment.NewLine +
                newBlock;

            return before + crossSectionText + after;
        }
    }
}
