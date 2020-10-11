using System;
using System.Collections.Generic;

namespace TBMDesigner.Domain
{
    public class VehicleData
    {
        public string VehicleType { get; set; }
        public List<double> HorizontalData { get; set; }
        public List<double> VerticalData { get; set; }
        public double BogieCentres { get; set; }
        public double BodyOverhang { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double MaximumBodyRolldeg { get; set; }
        public double Bounce { get; set; }
        public double LateralTolerance { get; set; }
        public double TrackGauge { get; set; }
    }
}
