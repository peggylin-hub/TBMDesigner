using System;
using System.Collections.Generic;
using System.Text;

namespace TBMDesigner.Domain
{
    public class RailSpec
    {
        public string Name { get; set; }
        public List<TrackSpecs> TrackSpecs { get; set; }
    }
}
