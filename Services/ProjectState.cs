using System;
using System.Collections.Generic;
using Brease.Core.Models;

namespace CBreaseWebApp1.Services
{
    public class ProjectState
    {
        public BreaseProject? CurrentProject { get; set; }

        public string? OriginalCbzText { get; set; }
        public string? OriginalCbzFileName { get; set; }

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
            LastSavedCrossSection = null;
            CrossSectionEditorPoints.Clear();
            NotifyStateChanged();
        }
    }
}