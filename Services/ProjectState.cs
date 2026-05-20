using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brease.Core.Models;
using Brease.Core.Readers;
using Brease.Core.Validation;

namespace CBreaseWebApp1.Services
{
    public class ProjectState
    {
        public BreaseProject? CurrentProject { get; set; }

        public string? OriginalCbzText { get; set; }
        public string? OriginalCbzFileName { get; set; }
        public List<CbzCrossSectionStationElevationValidationResult> CrossSectionStationElevationWarnings { get; set; } = new();

        public Section? LastSavedCrossSection { get; set; }

        public List<XData> CrossSectionEditorPoints { get; set; } = new();

        public event Action? OnChange;

        public bool HasProject => CurrentProject != null;

        public void NotifyStateChanged() => OnChange?.Invoke();

        public void Clear()
        {
            CurrentProject = null;
            OriginalCbzText = null;
            OriginalCbzFileName = null;
            CrossSectionStationElevationWarnings.Clear();
            LastSavedCrossSection = null;
            CrossSectionEditorPoints.Clear();
            NotifyStateChanged();
        }

        public void RefreshCrossSectionStationElevationWarningsFromText(string? cbzText)
        {
            CrossSectionStationElevationWarnings = CbzCrossSectionStationElevationValidator
                .ValidateLines(ReadLines(cbzText))
                .Where(result => result.IsMalformed)
                .ToList();
        }

        public void RefreshCrossSectionStationElevationWarningsFromLoadResult(CbzLoadResult? loadResult)
        {
            CrossSectionStationElevationWarnings = (loadResult?.CrossSectionStationElevationIssues
                    ?? Enumerable.Empty<CbzCrossSectionStationElevationValidationResult>())
                .Where(result => result.IsMalformed)
                .ToList();
        }

        public void ClearCrossSectionStationElevationWarnings()
        {
            CrossSectionStationElevationWarnings.Clear();
        }

        private static IEnumerable<string> ReadLines(string? text)
        {
            using var reader = new StringReader(text ?? string.Empty);
            string? line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
    }
}
