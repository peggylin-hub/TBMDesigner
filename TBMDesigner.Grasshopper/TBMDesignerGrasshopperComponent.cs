using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Grasshopper;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Geometry;
using TBMDesigner.Domain;
using TBMDesigner.Functions.KinematicEnvelope;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace TBMDesigner.Grasshopper
{
    public class TBMDesignerGrasshopperComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TBMDesignerGrasshopperComponent()
          : base("TBMDesigner.Grasshopper", "Nickname",
              "Description",
              "AECOM TBM", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Curve Type", "CT", "Type of Curve (Tangent, Curve)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Curve Radius", "CR", "Radius of Curve", GH_ParamAccess.list);
            pManager.AddNumberParameter("SuperElevation", "e", "Super Elevation on Point", GH_ParamAccess.list);
            pManager.AddTextParameter("Curve Side", "CS", "Side of Curve (Left, Right)", GH_ParamAccess.list);
            pManager.AddTextParameter("Track Sleeper Type", "ST", "Type of Sleeper (Wooden, Concrete, Slab_Track)", GH_ParamAccess.item);
            pManager.AddTextParameter("Specification Name", "SN", "Name of Specification", GH_ParamAccess.item);
            pManager.AddTextParameter("Vehicle Type", "VT", "Type of Vehicle", GH_ParamAccess.item);
            pManager.AddTextParameter("VehicleDataFilePath", "VDP", "Path of Vehicle Data JSON file", GH_ParamAccess.item);
            pManager.AddTextParameter("SpecificationPath", "SP", "Path of Specification JSON file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Vertical Track Tolerance", "VTT", "Vertical Track Tolerance", GH_ParamAccess.item);
            pManager.AddNumberParameter("Point Tolerance", "PT", "Tolerance for duplicated Point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("KE0 (Static)", "KE0", "Static Kinematic Envelop", GH_ParamAccess.list);
            pManager.AddCurveParameter("KE1 (Body Roll, Positive TT)", "KE1", "Kinematic Envelop (Body Roll, Positive TT)", GH_ParamAccess.list);
            pManager.AddCurveParameter("KE2 (Bounce Positive TT)", "KE2", "Kinematic Envelop (Bounce Positive TT)", GH_ParamAccess.list);
            pManager.AddCurveParameter("KE3 (Body Roll Negative TT)", "KE3", "Kinematic Envelop (Body Roll Negative TT)", GH_ParamAccess.list);
            pManager.AddCurveParameter("KE4 (Bounce, Negative TT)", "KE4", "Kinematic Envelop (Bounce, Negative TT)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Structural Gauge", "SG", "Kinematic Envelop (Structural Gauge)", GH_ParamAccess.list);
            pManager.AddTextParameter("Warnings", "WG", "Warning Message", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region //initiate variables
            List<string> curveTypes = new List<string>();
            List<double> curveRadiuses = new List<double>();
            List<double> superElevations = new List<double>();
            List<string> curveSides = new List<string>();
            SleeperType sleeperType;
            string sleeperTypeName = string.Empty;
            string SpecificationName = string.Empty;
            string vehicleType = string.Empty;
            string VechileDataFilePath = string.Empty;
            string SpecificationPath = string.Empty;
            bool VerticalTrackTolerance = false;
            double pointTolerance = 0.001;
            #endregion

            #region get data from input
            DA.GetDataList(0, curveTypes);
            DA.GetDataList(1, curveRadiuses);
            DA.GetDataList(2, superElevations);
            DA.GetDataList(3, curveSides);
            DA.GetData(4, ref sleeperTypeName);
            DA.GetData(5, ref SpecificationName);
            DA.GetData(6, ref vehicleType);
            DA.GetData(7, ref VechileDataFilePath);
            DA.GetData(8, ref SpecificationPath);
            DA.GetData(9, ref VerticalTrackTolerance);
            DA.GetData(10, ref pointTolerance);

            //if no data provided, do not do anything
            if (curveTypes.Count == 0 || curveRadiuses.Count == 0 || superElevations.Count == 0 || curveSides.Count == 0 || string.IsNullOrEmpty(sleeperTypeName) || string.IsNullOrEmpty(SpecificationName) 
                || string.IsNullOrEmpty(vehicleType) || string.IsNullOrEmpty(VechileDataFilePath))
                return;

            //if the sleeper type is not avlid, do not do anything
            try
            {
                sleeperType = (SleeperType)Enum.Parse(typeof(SleeperType), sleeperTypeName.ToUpper());
            }
            catch{ return; }
            
            int no = curveTypes.Count;
            string assPath = Assembly.GetExecutingAssembly().Location;

            //use default file, if not given
            if (string.IsNullOrEmpty(VechileDataFilePath))
                VechileDataFilePath = Path.GetDirectoryName(assPath) + "\\VehicleData.json";

            //use default file, if not given
            if (string.IsNullOrEmpty(SpecificationPath))
                SpecificationPath = Path.GetDirectoryName(assPath) + "\\RailcorpSpecs.json";

            //if the file is not found
            if (!File.Exists(VechileDataFilePath)) return;
            if (!File.Exists(SpecificationPath)) return;

            RailSpec specification = LoadSpecs(SpecificationPath);
            if (specification == null) return;

            TrackSpecs trackSpec = specification.TrackSpecs.Where(x => x.Type == SpecificationName).FirstOrDefault();

            if (trackSpec == null) return;
            #endregion

            #region get vehicle data from spec and by selection
            List<VehicleData> vehicleDatas = LoadVehicleData(VechileDataFilePath);

            VehicleData vehicleData = null;
            if (vehicleDatas.Select(x => x.VehicleType).ToList().Contains(vehicleType))
                vehicleData = vehicleDatas.Where(x => x.VehicleType == vehicleType).FirstOrDefault();

            //terminate if no vechicle data is load
            if (vehicleData == null) return;
            #endregion

            List<Polyline> staticKE_pl = new List<Polyline>();
            List<Polyline> KE1_pl = new List<Polyline>();
            List<Polyline> KE2_pl = new List<Polyline>();
            List<Polyline> KE3_pl = new List<Polyline>();
            List<Polyline> KE4_pl = new List<Polyline>();
            List<Polyline> structuralGauge_pl = new List<Polyline>();
            List<string> warnings = new List<string>();

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
                    warnings.Add("Curve Type string is not valid. It must be 'Curve' or 'Tagent'");
                    continue;
                }

                try
                {
                    cvSide = (CurveSide)Enum.Parse(typeof(CurveSide), curveSides[i].ToUpper());
                }
                catch 
                { 
                    warnings.Add("Curve Type Side is not valid. It must be 'Left' or 'Right'");
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
                    double[][] _KE0 = (double[][]) data["Static"];//data unit is mm
                    double[][] _ke1 = (double[][]) data["KE1"];//data unit is mm
                    double[][] _ke2 = (double[][]) data["KE2"];//data unit is mm
                    double[][] _ke3 = (double[][]) data["KE3"];//data unit is mm
                    double[][] _ke4 = (double[][]) data["KE4"];//data unit is mm
                    double[][] _ke5 = (double[][]) data["Structural Gauge"]; //data unit is mm
                    string[]_warning = (string[]) data["Warnings"];

                    List<Point3d> staticKE = new List<Point3d>();
                    List<Point3d> KE1 = new List<Point3d>();
                    List<Point3d> KE2 = new List<Point3d>();
                    List<Point3d> KE3 = new List<Point3d>();
                    List<Point3d> KE4 = new List<Point3d>();
                    List<Point3d> structuralGauge = new List<Point3d>();
                    List<Point3d> pts = new List<Point3d>();

                    for (int n = 0; n < _KE0[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_KE0[0][n] / 1000, _KE0[1][n] / 1000, 0);
                        staticKE.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(staticKE, pointTolerance).ToList();
                    pts.Add(new Point3d(_KE0[0][0] / 1000, _KE0[1][0] / 1000, 0));
                    Polyline pl = new Polyline(pts);
                    staticKE_pl.Add(pl);

                    for (int n = 0; n < _ke1[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_ke1[0][n] / 1000, _ke1[1][n] / 1000, 0);
                        KE1.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(KE1, pointTolerance).ToList();
                    pts.Add(new Point3d(_ke1[0][0] / 1000, _ke1[1][0] / 1000, 0));
                    pl = new Polyline(pts);
                    KE1_pl.Add(pl);

                    for (int n = 0; n < _ke2[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_ke2[0][n] / 1000, _ke2[1][n] / 1000, 0);
                        KE2.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(KE2, pointTolerance).ToList();
                    pts.Add(new Point3d(_ke2[0][0] / 1000, _ke2[1][0] / 1000, 0));
                    pl = new Polyline(pts);
                    KE2_pl.Add(pl);

                    for (int n = 0; n < _ke3[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_ke3[0][n] / 1000, _ke3[1][n] / 1000, 0);
                        KE3.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(KE3, pointTolerance).ToList();
                    pts.Add(new Point3d(_ke3[0][0] / 1000, _ke3[1][0] / 1000, 0));//add the first point
                    pl = new Polyline(pts);
                    KE3_pl.Add(pl);

                    for (int n = 0; n < _ke4[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_ke4[0][n] / 1000, _ke4[1][n] / 1000, 0);
                        KE4.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(KE4, pointTolerance).ToList();
                    pts.Add(new Point3d(_ke4[0][0] / 1000, _ke4[1][0] / 1000, 0));//add the first point
                    pl = new Polyline(pts);
                    KE4_pl.Add(pl);

                    for (int n = 0; n < _ke5[0].Count(); n++)
                    {
                        Point3d pt = new Point3d(_ke5[0][n] / 1000, _ke5[1][n] / 1000, 0);
                        structuralGauge.Add(pt);
                    }
                    pts = Point3d.CullDuplicates(structuralGauge, pointTolerance).ToList();
                    pts.Add(new Point3d(_ke5[0][0] / 1000, _ke5[1][0] / 1000, 0));//add the first point
                    pl = new Polyline(pts);
                    structuralGauge_pl.Add(pl);

                    warnings.AddRange(_warning.ToList());
                }
                else
                {
                    staticKE_pl[i] = null;
                    KE1_pl[i] = null;
                    KE2_pl[i] = null;
                    KE3_pl[i] = null;
                    KE4_pl[i] = null;
                    structuralGauge_pl[i] = null;
                    warnings[i] = null;
                }
            }

            DA.SetDataList(0, staticKE_pl);
            DA.SetDataList(1, KE1_pl);
            DA.SetDataList(2, KE2_pl);
            DA.SetDataList(3, KE3_pl);
            DA.SetDataList(4, KE4_pl);
            DA.SetDataList(5, structuralGauge_pl);
            DA.SetDataList(6, warnings);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0414d69c-e5ed-4536-bc5c-4d98c2ceb918"); }
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
    }
}
