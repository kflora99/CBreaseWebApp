using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Brease.Core.Models;
using Brease.Core.Readers;
using Brease.Core.Services;
using Brease.Core.Validation;

namespace CBreaseWebApp1.Services
{
    public class CbzFileService
    {
        public CbzExportValidationResult ValidateExportText(string cbzText, string? fileNameHint = null)
        {
            if (string.IsNullOrWhiteSpace(cbzText))
            {
                return CbzExportValidationResult.Failure(
                    "Export validation failed. The file was not saved.",
                    "Generated CBZ text is empty.");
            }

            var loader = new CbzBreaseProjectLoader();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cbzText));
            var result = loader.TryLoadFromStream(
                stream,
                fileNameHint,
                new CbzLoadOptions { ValidateCrossSectionStationElevation = true });

            if (!result.Succeeded)
            {
                return CbzExportValidationResult.Failure(
                    "Export validation failed. The file was not saved.",
                    result.ErrorMessage ?? "The generated CBZ could not be loaded.");
            }

            var malformedIssues = result.CrossSectionStationElevationIssues
                .Where(issue => issue.IsMalformed)
                .ToList();

            var compatibilityNotes = CbzCompatibilityNoteService.BuildNotes(
                result.Project!,
                cbzText,
                malformedIssues);

            if (compatibilityNotes.Count > 0)
            {
                var details = CbzCompatibilityNoteService.FormatNoteSummary(
                    compatibilityNotes,
                    maxRows: 5);

                return CbzExportValidationResult.Warning(
                    "Export validation passed with compatibility notes. The CBZ file was generated successfully.",
                    details);
            }

            return CbzExportValidationResult.Success(
                "Export validation passed. The CBZ file was generated successfully.");
        }

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

    public sealed class CbzExportValidationResult
    {
        private CbzExportValidationResult(bool succeeded, bool hasWarnings, string message, string? details)
        {
            Succeeded = succeeded;
            HasWarnings = hasWarnings;
            Message = message;
            Details = details;
        }

        public bool Succeeded { get; }
        public bool HasWarnings { get; }
        public string Message { get; }
        public string? Details { get; }

        public static CbzExportValidationResult Success(string message)
        {
            return new CbzExportValidationResult(true, false, message, null);
        }

        public static CbzExportValidationResult Warning(string message, string details)
        {
            return new CbzExportValidationResult(true, true, message, details);
        }

        public static CbzExportValidationResult Failure(string message, string details)
        {
            return new CbzExportValidationResult(false, false, message, details);
        }
    }
}
