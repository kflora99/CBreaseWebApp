using System;
using System.Collections.Generic;
using Brease.Core.Models;

namespace CBreaseWebApp1.Models
{
    public class CrossSectionDraft
    {
        public string? ProjectFileName { get; set; }
        public DateTime SavedAtUtc { get; set; }

        public Section? Section { get; set; }

        public List<XData> Points { get; set; } = new();

        public XData? CurrentPointDraft { get; set; }
    }
}