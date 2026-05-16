using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Brease.Core.Models;
using Brease.Core.Services;

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

            var rebuiltCrossSectionText = CbzCrossSectionSerializer.BuildCrossSectionSection(sectionList);

            return before + rebuiltCrossSectionText + after;
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

            string newBlock = CbzCrossSectionSerializer.BuildCrossSectionBlock(newSection);

            crossSectionText =
                crossSectionText.TrimEnd('\r', '\n') +
                Environment.NewLine +
                Environment.NewLine +
                newBlock;

            return before + crossSectionText + after;
        }
    }
}
