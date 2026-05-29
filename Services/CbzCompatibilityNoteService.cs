using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Brease.Core.Models;
using Brease.Core.Validation;

namespace CBreaseWebApp1.Services
{
    public static class CbzCompatibilityNoteService
    {
        public static List<string> BuildNotes(
            BreaseProject project,
            string? cbzText,
            IReadOnlyList<CbzCrossSectionStationElevationValidationResult>? stationElevationWarnings)
        {
            var notes = new List<string>();
            var raw = AnalyzeCompatibilityText(cbzText);

            var incompletePileCount = project.Substructures?
                .Count(sub => sub.PileTipElevation.HasValue && !IsCompletePileGeometry(sub)) ?? 0;
            if (incompletePileCount > 0)
            {
                notes.Add(
                    $"Some pile geometry is incomplete. Pile tip elevations are known, but pile width/count/spacing are missing or invalid for {incompletePileCount} substructures.");
            }

            if (raw.MissingColumnDataRows > 0)
            {
                notes.Add(
                    $"Some substructures have missing column geometry. Column information data rows are absent for {raw.MissingColumnDataRows} substructures.");
            }

            if (raw.MissingSectionTypes > 0 || raw.MissingRefConstantElevations > 0)
            {
                var pieces = new List<string>();
                if (raw.MissingSectionTypes > 0)
                    pieces.Add("Section Type = Channel X-Section");

                if (raw.MissingRefConstantElevations > 0)
                    pieces.Add("Ref Constant Elevation = No");

                var affectedCount = Math.Max(raw.MissingSectionTypes, raw.MissingRefConstantElevations);
                notes.Add(
                    $"Some cross-sections use an older header format. The app applied standard defaults for missing fields in {affectedCount} cross-section{Plural(affectedCount)}: " +
                    string.Join(" and ", pieces) + ".");
            }

            if ((project.ConstantStructuralDepth ?? 0.0) == 0.0 && (project.VsdTemplates?.Count ?? 0) == 0)
            {
                notes.Add("Structural depth is 0 and no variable structural depth templates are defined.");
            }

            var zeroPointSections = project.CrossSections?.Count(section => section.Data == null || section.Data.Count == 0) ?? 0;
            if (zeroPointSections > 0)
            {
                notes.Add(
                    $"{zeroPointSections} cross-section{Plural(zeroPointSections)} loaded with no point data.");
            }

            var malformedStationElevationRows = stationElevationWarnings?.Count(issue => issue.IsMalformed) ?? 0;
            if (malformedStationElevationRows > 0)
            {
                notes.Add(
                    $"{malformedStationElevationRows} cross-section point row{Plural(malformedStationElevationRows)} have blank or non-numeric Station/Elev. values.");
            }

            return notes;
        }

        public static string FormatNoteSummary(IReadOnlyList<string> notes, int maxRows = 5)
        {
            if (notes == null || notes.Count == 0)
                return string.Empty;

            var visible = notes.Take(maxRows).ToList();
            var lines = visible.Select(note => "- " + note).ToList();
            var remaining = notes.Count - visible.Count;
            if (remaining > 0)
                lines.Add("- " + remaining.ToString(CultureInfo.InvariantCulture) + " more compatibility note" + Plural(remaining) + ".");

            return string.Join(Environment.NewLine, lines);
        }

        private static bool IsCompletePileGeometry(SubstructureItem sub)
        {
            if (!sub.PileTipElevation.HasValue)
                return false;

            if (!TryParsePositiveDouble(sub.PileWidth, out _))
                return false;

            if (!TryParsePositiveInt(sub.PileNumber, out var pileCount))
                return false;

            if (!TryParseNonNegativeDouble(sub.PileSpacing, out var pileCenterSpacing))
                return false;

            return pileCount == 1 || pileCenterSpacing > 0.0;
        }

        private static CompatibilityTextSummary AnalyzeCompatibilityText(string? cbzText)
        {
            var summary = new CompatibilityTextSummary();
            if (string.IsNullOrWhiteSpace(cbzText))
                return summary;

            var lines = cbzText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();

                if (trimmed.Equals("Column Information", StringComparison.OrdinalIgnoreCase))
                {
                    var dataIndex = i + 2;
                    if (dataIndex < lines.Length &&
                        lines[dataIndex].Trim().Equals("Footing Information", StringComparison.OrdinalIgnoreCase))
                    {
                        summary.MissingColumnDataRows++;
                    }
                }
            }

            AnalyzeCrossSectionHeaderCompatibility(lines, summary);

            return summary;
        }

        private static void AnalyzeCrossSectionHeaderCompatibility(string[] lines, CompatibilityTextSummary summary)
        {
            var inCrossSectionData = false;
            var current = new CrossSectionHeaderCompatibility();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Equals("#CROSS SECTION DATA#", StringComparison.OrdinalIgnoreCase))
                {
                    inCrossSectionData = true;
                    current = new CrossSectionHeaderCompatibility();
                    continue;
                }

                if (!inCrossSectionData)
                    continue;

                if (trimmed.StartsWith("#", StringComparison.Ordinal) &&
                    !trimmed.Equals("#CROSS SECTION DATA#", StringComparison.OrdinalIgnoreCase))
                {
                    FinishCrossSectionHeaderCompatibility(summary, current);
                    return;
                }

                if (trimmed.StartsWith("Cross Section Date=", StringComparison.OrdinalIgnoreCase))
                {
                    FinishCrossSectionHeaderCompatibility(summary, current);
                    current = new CrossSectionHeaderCompatibility { HasHeader = true };
                    continue;
                }

                if (!current.HasHeader)
                    continue;

                if (trimmed.StartsWith("Section Type=", StringComparison.OrdinalIgnoreCase))
                    current.HasSectionType = true;
                else if (trimmed.StartsWith("Ref Constant Elevation=", StringComparison.OrdinalIgnoreCase))
                    current.HasRefConstantElevation = true;
            }

            FinishCrossSectionHeaderCompatibility(summary, current);
        }

        private static void FinishCrossSectionHeaderCompatibility(
            CompatibilityTextSummary summary,
            CrossSectionHeaderCompatibility current)
        {
            if (!current.HasHeader)
                return;

            if (!current.HasSectionType)
                summary.MissingSectionTypes++;

            if (!current.HasRefConstantElevation)
                summary.MissingRefConstantElevations++;
        }

        private static bool TryParsePositiveInt(string? value, out int result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var text = value.Trim();
            if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var intValue))
            {
                result = intValue;
                return result > 0;
            }

            if (!double.TryParse(
                    text,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var numericValue) ||
                double.IsNaN(numericValue) ||
                double.IsInfinity(numericValue) ||
                numericValue <= 0.0 ||
                numericValue > int.MaxValue)
            {
                return false;
            }

            result = (int)numericValue;
            return result > 0;
        }

        private static bool TryParsePositiveDouble(string? value, out double result)
        {
            return TryParseDouble(value, out result) && result > 0.0;
        }

        private static bool TryParseNonNegativeDouble(string? value, out double result)
        {
            return TryParseDouble(value, out result) && result >= 0.0;
        }

        private static bool TryParseDouble(string? value, out double result)
        {
            result = 0.0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            return double.TryParse(
                    value.Trim(),
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out result) &&
                !double.IsNaN(result) &&
                !double.IsInfinity(result);
        }

        private static string Plural(int count)
        {
            return count == 1 ? string.Empty : "s";
        }

        private sealed class CompatibilityTextSummary
        {
            public int MissingSectionTypes { get; set; }
            public int MissingRefConstantElevations { get; set; }
            public int MissingColumnDataRows { get; set; }
        }

        private sealed class CrossSectionHeaderCompatibility
        {
            public bool HasHeader { get; set; }
            public bool HasSectionType { get; set; }
            public bool HasRefConstantElevation { get; set; }
        }
    }
}
