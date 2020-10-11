using System;
using System.Collections.Generic;
using System.Text;

namespace TBMDesigner.Domain
{
    public class TrackSpecs
    {
        public string Type { get; set; }
        public double LateralTangentTolerance { get; set; }
        public double LateralCurveTolerance { get; set; }
        public double RailWearLateral { get; set; }
        public double VerticalTrackTolerancePositive { get; set; }
        public double VerticalTrackToleranceNegative { get; set; }
        public double SuperTolerance { get; set; }
    }
}
