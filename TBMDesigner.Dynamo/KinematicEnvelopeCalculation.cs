using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;
using Newtonsoft.Json;
using TBMDesigner.Domain;
using TBMDesigner.Functions.KinematicEnvelope;

namespace TBMDesigner.Dynamo
{
    public class KinematicEnvelopeCalculation
    {
        [MultiReturn(new[] { "KE0 (Static)", "KE1 (Body Roll, Positive TT)", "KE2 (Bounce Positive TT)", "KE3 (Body Roll Negative TT)", "KE4 (Bounce, Negative TT)", "Structural Gauge", "Warnings" })]
        public static Dictionary<string, object[]> KinematicEnvelope(
            string[] curveTypes,
            double[] curveRadiuses,
            double[] superElevations,
            string[] curveSides,
            SleeperType sleeperType,
            string SpecificationName,
            string vehicleType,
            string VechileDataFilePath = "",
            string SpecificationPath = "",
            bool VerticalTrackTolerance = false)
        {
            int no = curveTypes.Count();
            string assPath = Assembly.GetExecutingAssembly().Location;

            if (VechileDataFilePath == "")
                VechileDataFilePath = Path.GetDirectoryName(assPath) + "\\VehicleData.json";

            if (SpecificationPath == "")
                SpecificationPath = Path.GetDirectoryName(assPath) + "\\RailcorpSpecs.json";

            RailSpec specification = LoadSpecs(SpecificationPath);
            if (specification == null)
                return null;

            TrackSpecs trackSpec = specification.TrackSpecs.Where(x => x.Type == SpecificationName).FirstOrDefault();

            #region get vehicle data from spec and by selection
            List<VehicleData> vehicleDatas = LoadVehicleData(VechileDataFilePath);

            VehicleData vehicleData = null;
            if (vehicleDatas.Select(x => x.VehicleType).ToList().Contains(vehicleType))
            {
                vehicleData = vehicleDatas.Where(x => x.VehicleType == vehicleType).FirstOrDefault();
            }

            //terminate if no vechicle data is load
            if (vehicleData == null)
                return null;
            #endregion

            Dictionary<string, object[]> output = new Dictionary<string, object[]>();

            object[] staticKE = new object[no];
            object[] KE1 = new object[no];
            object[] KE2 = new object[no];
            object[] KE3 = new object[no];
            object[] KE4 = new object[no];
            object[] structuralGauge = new object[no];
            object[] warnings = new object[no];

            for (int i = 0; i < no; i++)
            {
                CurveType cvType = CurveType.CURVE;
                CurveSide cvSide = CurveSide.NONE;
                try
                {
                    cvType = (CurveType)Enum.Parse(typeof(CurveType), curveTypes[i].ToUpper());
                }
                catch 
                { 
                    warnings[i] = "Curve Type string is not valid. It must be 'Curve' or 'Tagent'";
                    continue;
                }

                try
                {
                    cvSide = (CurveSide)Enum.Parse(typeof(CurveSide), curveSides[i].ToUpper());
                }
                catch 
                { 
                    warnings[i] = "Curve Type Side is not valid. It must be 'Left' or 'Right'";
                    continue;
                }

                double cvRadius = curveRadiuses[i];
                double e = superElevations[i];

                //calculate each 
                Dictionary<string, object> data = KEcalculation.KinematicEnvelopeCal(
                   vehicleData,
                   sleeperType,
                   trackSpec,
                   VerticalTrackTolerance,
                   cvType,
                   cvRadius,
                   e,
                   cvSide);

                if (data != null)
                {
                    staticKE[i] = data["Static"];
                    KE1[i] = data["KE1"];
                    KE2[i] = data["KE2"];
                    KE3[i] = data["KE3"];
                    KE4[i] = data["KE4"];
                    structuralGauge[i] = data["Structural Gauge"];
                    warnings[i] = data["Warnings"];
                }
                else
                {
                    staticKE[i] = null;
                    KE1[i] = null;
                    KE2[i] = null;
                    KE3[i] = null;
                    KE4[i] = null;
                    structuralGauge[i] = null;
                    warnings[i] = null;
                }
            }

            output.Add("KE0 (Static)", staticKE);
            output.Add("KE1 (Body Roll, Positive TT)", KE1);
            output.Add("KE2 (Bounce Positive TT)", KE2);
            output.Add("KE3 (Body Roll Negative TT)", KE3);
            output.Add("KE4 (Bounce, Negative TT)", KE4);
            output.Add("Structural Gauge", structuralGauge);
            output.Add("Warnings", warnings);

            return output;
        }

        private static List<VehicleData> LoadVehicleData(string path)
        {
            string json = File.ReadAllText(path);
            List<VehicleData> vCollection = (List<VehicleData>)JsonConvert.DeserializeObject(json, typeof(List<VehicleData>));
            return vCollection;
        }

        private static RailSpec LoadSpecs(string path)
        {
            //string assPath = Assembly.GetExecutingAssembly().Location;
            //string path = Path.GetDirectoryName(assPath) + "\\RailcorpSpecs.json";
            string json = File.ReadAllText(path);
            RailSpec spec = (RailSpec)JsonConvert.DeserializeObject(json, typeof(RailSpec));
            return spec;
        }

        public static string[] TrackSpecification(string SpecJsonFilePath)
        {
            RailSpec specification = LoadSpecs(SpecJsonFilePath);
            string[] trackSpecs = specification.TrackSpecs.Select(x => x.Type).ToArray();
            return trackSpecs;
        }

        public static string[] VechicleTypeList(string VechicleJsonFilePath)
        {
            List<VehicleData> vCollection = LoadVehicleData(VechicleJsonFilePath);
            return vCollection.Select(x => x.VehicleType).ToArray();
        }
    }
}
