using System;
using System.Collections.Generic;
using System.Text;

namespace TBMDesigner.Functions.Helper
{
    public class DataConvert
    {
        public static double RadToDeg(double x) => x = x * 180 / Math.PI;

        public static double DegToRad(double x) => x = x * Math.PI / 180;

    }

}
