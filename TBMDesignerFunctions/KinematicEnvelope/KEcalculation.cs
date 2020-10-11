using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TBMDesigner.Functions.Helper;
using TBMDesigner.Domain;

namespace TBMDesigner.Functions.KinematicEnvelope
{
    public class KEcalculation
    {
        #region private properties
        private static double _MaxSuperOnTangentAllowed = 20; //Add to constant, max radius allowed
        private static double _HorizontalPivot = 0; //Add to constant
        private static double _VerticalPivot = 610; //Add to constant
        private static double _MaxRadius = 2000; //maximum radius

        private static double _TrackGauge;
        private static double _RailCentres; //calculate from _TrackGauge
        private static double _SuperElevationRunningEdge;
        private static double _Superelevation;
        private static double _BogieCentres;
        private static double _RollingStockLateralTolerance;
        private static double _VehicleLength;
        //private static double _BodyOverhang;
        private static double _VehicleWidth;
        private static double _MaxBodyRollDeg;
        private static double _MaxBodyRollRad;
        private static double _Bounce;
        private static double _HalfHeight;
        private static double _LateralToleranceTang;
        private static double _LateralToleranceCurve;
        private static double _VerticalTolerancePos;
        private static double _VerticalToleranceNeg;
        private static double _RailWearLateral;
        private static double _SuperTolerance;
        //private static double _LataralRailTolerance;
        private static double _RotAngleToSuperTolRad;
        //private static double _RotationToSuperDeg;
        private static double _RotAngToSuper;
        //private static double _AngleToSuperDeg;//not used
        private static double _SuperToleranceRunningEdge;
        private static List<double> _HorizontalData;
        private static List<double> _VerticalData;
        private static List<string> _WarningMegs;

        private static double _yValueLowRail = 0;
        private static double _xValueLowRail = 0;
        private static int _noOfPoint = 0;
        #endregion

        /// <summary>
        /// Refresh the data in the main report
        /// the calculation is based on excel calculation sheet KE_Calculator_v2.3.xlsm (7/03/2013)
        /// </summary>
        public static Dictionary<string, object> KinematicEnvelopeCal(
            VehicleData vehicleData,
            SleeperType sleeperType,
            TrackSpecs trackSpec,
            bool VerticalTrackTolerance,
            CurveType curveType,
            double curveRadius,
            double e,
            CurveSide curveSide)
        {
            #region properties
            _Superelevation = e;
            _WarningMegs = new List<string>();
            #endregion

            #region load specification => track constants
            
            if (trackSpec == null)
                return null;

            //get the properties from spec
            _LateralToleranceTang = trackSpec.LateralTangentTolerance;
            _LateralToleranceCurve = trackSpec.LateralCurveTolerance;
            _RailWearLateral = trackSpec.RailWearLateral;
            _SuperTolerance = trackSpec.SuperTolerance;

            /////?========================????????? Calculations => G6, G7 
            if (VerticalTrackTolerance == true)
            {
                _VerticalTolerancePos = trackSpec.VerticalTrackTolerancePositive;
                _VerticalToleranceNeg = trackSpec.VerticalTrackToleranceNegative;
            }
            else
            {
                _VerticalTolerancePos = 0;
                _VerticalToleranceNeg = 0;
            }

            //load vehicle data for parameters
            GetVehicleData(vehicleData);

            //provide warning if superelecation > 20 in tangent
            //Calculation G10
            if (curveType == CurveType.TANGENT && _Superelevation > _MaxSuperOnTangentAllowed)
            {
                _WarningMegs.Add("Superelevation is greater than 20.");
                //_LataralRailTolerance = trackSpec.LateralTangentTolerance;
            }
            else
            {
                _WarningMegs.Add("");
                //_LataralRailTolerance = trackSpec.LateralCurveTolerance;
            }
                
            _RailCentres = _TrackGauge + 70;
            _SuperElevationRunningEdge = _Superelevation * _TrackGauge / _RailCentres;

            if (curveSide == CurveSide.NONE)
            {
                _RotAngToSuper = 0;
                //_AngleToSuperDeg = 0;
            }
            else if (curveSide == CurveSide.LEFT)
                _RotAngToSuper = Math.Asin(_SuperElevationRunningEdge / _TrackGauge);
            else
                _RotAngToSuper = -Math.Asin(_SuperElevationRunningEdge / _TrackGauge);

            _SuperToleranceRunningEdge = Math.Round(_SuperTolerance * (_TrackGauge / _RailCentres), 4);
            _RotAngleToSuperTolRad = Math.Round(Math.Asin((_SuperTolerance * _TrackGauge / _RailCentres) / _TrackGauge), 8);
            //_RotationToSuperDeg = Math.Round(DataConvert.RadToDeg( _RotAngleToSuperTolRad), 8);

            if (curveSide == CurveSide.LEFT)
                _xValueLowRail = -_TrackGauge / 2;
            else if (curveSide == CurveSide.RIGHT)
                _xValueLowRail = _TrackGauge / 2;
            #endregion

            double x1 = _SuperElevationRunningEdge * Math.Tan(_RotAngToSuper / 2);//x' //Calculation => F22
            double y1 = x1 * _SuperElevationRunningEdge / _TrackGauge;//y' //Calculation => G22
            _noOfPoint = _HorizontalData.Count;//number of profile points

            #region define variables
            double alphaRad;
            double alphaDeg;

            List<double> deltaX = new List<double>();
            List<double> deltaY = new List<double>();
            //List<double> _xFirst = new List<double>();
            //List<double> _yFirst = new List<double>();
            List<double> _deltaX1 = new List<double>();
            List<double> _deltaY1 = new List<double>();
            List<double> _tetaRad = new List<double>();
            List<double> _tetaDeg = new List<double>();

            List<double> xFirstList = new List<double>();
            List<double> yFirstList = new List<double>();
            List<double> x2List = new List<double>();
            List<double> y2List = new List<double>();
            List<double> x3List = new List<double>();
            List<double> y3List = new List<double>();
            //List<double> x3NegList = new List<double>();
            //List<double> y3NegList = new List<double>();
            List<double> x4List = new List<double>();
            List<double> y4List = new List<double>();
            //List<double> x4NegList = new List<double>();
            //List<double> y4NegList = new List<double>();
            List<double> centreThrowList = new List<double>();
            List<double> endThrowList = new List<double>();
            List<double> x5List = new List<double>();
            List<double> y5List = new List<double>();
            List<double> x5NegList = new List<double>();
            List<double> y5NegList = new List<double>();
            List<double> xpList = new List<double>();
            List<double> ypList = new List<double>();
            List<double> xpNegList = new List<double>();
            List<double> ypNegList = new List<double>();
            List<double> RList = new List<double>();
            List<double> RNegList = new List<double>();
            List<double> bodyRollTetaRadList = new List<double>();
            List<double> bodyRollTetaDegList = new List<double>();
            List<double> bodyRollTetaRadNegList = new List<double>();
            List<double> bodyRollTetaDegNegList = new List<double>();
            List<double> bodyRollTetaFirstRadList = new List<double>();
            List<double> bodyRollTetaFirstDegList = new List<double>();
            List<double> bodyRollTetaFirstRadNegList = new List<double>();
            List<double> bodyRollTetaFirstDegNegList = new List<double>();
            List<double> bodyRollx = new List<double>();
            List<double> bodyRolly = new List<double>();
            List<double> bodyRollNegx = new List<double>();
            List<double> bodyRollNegy = new List<double>();
            List<double> bounceTetaRadList = new List<double>();
            List<double> bounceTetaDegList = new List<double>();
            //List<double> bounceTetaRadNegList = new List<double>();
            //List<double> bounceTetaDegNegList = new List<double>();
            List<double> bounceListx = new List<double>();
            List<double> bounceListy = new List<double>();
            //List<double> bounceListNegx = new List<double>();
            //List<double> bounceListNegy = new List<double>();

            #region Static Envelop
            List<double> staticKEx = new List<double>();
            List<double> staticKEy = new List<double>();
            double[][] staticKE = new double[2][];
            #endregion

            #region KE1 Body Roll Positive TT
            List<double> KE1_x = new List<double>();
            List<double> KE1_y = new List<double>();
            double[][] KE1 = new double[2][];
            #endregion

            #region KE2 (Bounce Positive TT)
            List<double> KE2_x = new List<double>();
            List<double> KE2_y = new List<double>();
            double[][] KE2 = new double[2][];
            #endregion

            #region KE3 Body Roll Negative TT
            List<double> KE3_x = new List<double>();
            List<double> KE3_y = new List<double>();
            double[][] KE3 = new double[2][];
            #endregion

            #region KE4 (Bounce Negative TT)
            List<double> KE4_x = new List<double>();
            List<double> KE4_y = new List<double>();
            double[][] KE4 = new double[2][];
            #endregion

            #region structural gauge
            List<double> structuralGaugeX = new List<double>();
            List<double> structuralGaugeY = new List<double>();
            double[][] structuralGauge = new double[2][];
            #endregion

            double centreThrow = 0;
            double endThrow = 0;
            #endregion

            for (int i = 0; i < _noOfPoint; i++)
            {
                #region Superelevation
                //H22
                double dx = ((_TrackGauge / 2 - _HorizontalData[i]) / _TrackGauge) * x1 + _VerticalData[i] * Math.Sin(_RotAngToSuper);
                //I22
                double dy = 0;

                if (_Superelevation != 0)
                    dy = (_TrackGauge / 2 - _HorizontalData[i]) * Math.Sin(_RotAngToSuper) - y1 * (_VerticalData[i] / _SuperElevationRunningEdge);

                deltaX.Add(dx);
                deltaY.Add(dy);

                //J22
                double x_1 = _xValueLowRail + (_HorizontalData[i] - _xValueLowRail) * Math.Cos(_RotAngToSuper) - (_VerticalData[i] - _yValueLowRail) * Math.Sin(_RotAngToSuper);
                //K22
                double y_1 = _yValueLowRail + (_HorizontalData[i] - _xValueLowRail) * Math.Sin(_RotAngToSuper) + (_VerticalData[i] - _yValueLowRail) * Math.Cos(_RotAngToSuper);

                staticKEx.Add(x_1);
                staticKEy.Add(y_1);

                //N22
                if (i < _noOfPoint / 2)
                    alphaRad = _RotAngToSuper + _RotAngleToSuperTolRad;
                else
                    alphaRad = _RotAngToSuper - _RotAngleToSuperTolRad;
                //O22
                alphaDeg = DataConvert.RadToDeg(alphaRad);

                double xFirst = 0;//P22 (x')
                double yFirst = 0;//Q22 (y')
                //P22
                if (i < _noOfPoint / 2)
                    xFirst = (_SuperTolerance * _TrackGauge / 1505) * Math.Tan(_RotAngleToSuperTolRad / 2);
                else
                    xFirst = (_SuperTolerance * _TrackGauge / 1505) * Math.Tan(-_RotAngleToSuperTolRad / 2);
                //Q22
                yFirst = xFirst * (_SuperTolerance * _TrackGauge / 1505) / _TrackGauge;

                xFirstList.Add(xFirst);
                yFirstList.Add(yFirst);

                double deltaX1 = 0;//R22
                double deltaY1 = 0;//S22
                if (i < _noOfPoint / 2)
                {
                    deltaX1 = ((_TrackGauge / 2 - x_1) / _TrackGauge) * -xFirst + y_1 * Math.Sin(_RotAngleToSuperTolRad);
                    deltaY1 = (_TrackGauge / 2 - x_1) * Math.Sin(0) + yFirst * y_1 / ((_SuperTolerance * _TrackGauge / 1505));
                }
                else
                {
                    deltaX1 = ((_TrackGauge / 2 - x_1) / _TrackGauge) * -xFirst + y_1 * Math.Sin(-_RotAngleToSuperTolRad);
                    deltaY1 = (_TrackGauge / 2 - x_1) * Math.Sin(0) - yFirst * y_1 / ((_SuperTolerance * _TrackGauge / 1505));
                }

                _deltaX1.Add(deltaX1);
                _deltaY1.Add(deltaY1);
                //A value is always null in the Excel spreadsheet

                #endregion

                #region Super Tolerance Case Generation
                double x2;//T22
                double y2;//U22

                if (_Superelevation == 0)
                {
                    x2 = GetSuperToleranceCase(_HorizontalData[i], _VerticalData[i], i)[0];
                    y2 = GetSuperToleranceCase(_HorizontalData[i], _VerticalData[i], i)[1];
                }
                else
                {
                    if (i < _noOfPoint / 2)
                    {
                        x2 = _xValueLowRail + (x_1 - _xValueLowRail) * Math.Cos(_RotAngleToSuperTolRad) - (y_1 - _yValueLowRail) * Math.Sin(_RotAngleToSuperTolRad);
                        y2 = _yValueLowRail + (x_1 - _xValueLowRail) * Math.Sin(_RotAngleToSuperTolRad) + (y_1 - _yValueLowRail) * Math.Cos(_RotAngleToSuperTolRad);
                    }
                    else
                    {
                        x2 = _xValueLowRail + (x_1 - _xValueLowRail) * Math.Cos(-_RotAngleToSuperTolRad) - (y_1 - _yValueLowRail) * Math.Sin(-_RotAngleToSuperTolRad);
                        y2 = _yValueLowRail + (x_1 - _xValueLowRail) * Math.Sin(-_RotAngleToSuperTolRad) + (y_1 - _yValueLowRail) * Math.Cos(-_RotAngleToSuperTolRad);
                    }

                }
                x2List.Add(x2);
                y2List.Add(y2);
                #endregion

                //factor
                int factor = 1;
                if (i >= _noOfPoint / 2)
                    factor = -1;

                #region Lateral Track Tolerance - Positive Vertical Track Tolerance
                double x3;//V22
                if (_Superelevation == 0 || curveRadius > _MaxRadius)
                    x3 = x2 - _LateralToleranceTang * factor;
                else
                    x3 = x2 - _LateralToleranceCurve * factor;
                double y3 = y2 + _VerticalTolerancePos;//W22

                x3List.Add(x3);
                y3List.Add(y3);
                #endregion

                #region Negative Vertical Track Tolerance
                //W55
                double y3Neg = y2 - _VerticalToleranceNeg;
                #endregion  

                #region Vehicle Tolerance (Lateral Track + Bogie Wear)
                double tetaRad = -alphaRad;//X22
                double tetaDeg = DataConvert.RadToDeg(tetaRad);//Y22

                _tetaRad.Add(tetaRad);
                _tetaDeg.Add(tetaDeg);

                //Z22
                double x4 = x3 - (_RollingStockLateralTolerance + _RailWearLateral) * Math.Cos(tetaRad) * factor;
                //Y22
                double y4 = y3 + (_RollingStockLateralTolerance + _RailWearLateral) * Math.Sin(tetaRad) * factor;

                x4List.Add(x4);
                y4List.Add(y4);
                #endregion

                #region Negative Vehicle Tolerance (Lateral Track + Bogie Wear)
                //Z55
                double x4Neg = x3 - (_RollingStockLateralTolerance + _RailWearLateral) * Math.Cos(tetaRad) * factor;
                //Y55
                double y4Neg = y3Neg + (_RollingStockLateralTolerance + _RailWearLateral) * Math.Sin(tetaRad) * factor;
                #endregion

                #region Centre / End Throw
                if (curveRadius != 0)
                {
                    centreThrow = Math.Pow(_BogieCentres, 2) / (8 * curveRadius * 1000); //ESC-215 (12.2.1)
                    endThrow = Math.Pow(_VehicleLength, 2) / ((8 * curveRadius * 1000) + (4 * _VehicleWidth)) - centreThrow;//ESC-215 (12.2.1)
                }
                else
                {
                    centreThrow = 0;
                    endThrow = 0;
                }

                centreThrowList.Add(centreThrow);
                endThrowList.Add(endThrow);

                double x5 = 0;//AD22
                double y5 = 0;//AE22
                double x5Neg = 0;//AD55
                double y5Neg = 0;//AE55

                ////// ====================================> this part is in doubt, should question the 
                if (curveSide == CurveSide.RIGHT)
                {
                    if (i < _noOfPoint / 2)
                    {
                        x5 = x4 - endThrow * Math.Cos(tetaRad) * factor;
                        y5 = y4 + endThrow * Math.Sin(tetaRad) * factor;
                        x5Neg = x4Neg - endThrow * Math.Cos(tetaRad) * factor;
                        y5Neg = y4Neg + endThrow * Math.Sin(tetaRad) * factor;
                    }
                    else
                    {
                        x5 = x4 - centreThrow * Math.Cos(tetaRad) * factor;
                        y5 = y4 + centreThrow * Math.Sin(tetaRad) * factor;
                        x5Neg = x4Neg - centreThrow * Math.Cos(tetaRad) * factor;
                        y5Neg = y4Neg + centreThrow * Math.Sin(tetaRad) * factor;
                    }
                }
                else
                {
                    if (i < _noOfPoint / 2)
                    {
                        x5 = x4 - centreThrow * Math.Cos(tetaRad) * factor;
                        y5 = y4 + centreThrow * Math.Sin(tetaRad) * factor;
                        x5Neg = x4Neg - centreThrow * Math.Cos(tetaRad) * factor;
                        y5Neg = y4Neg + centreThrow * Math.Sin(tetaRad) * factor;
                    }
                    else
                    {
                        x5 = x4 - endThrow * Math.Cos(tetaRad) * factor;
                        y5 = y4 + endThrow * Math.Sin(tetaRad) * factor;
                        x5Neg = x4Neg - endThrow * Math.Cos(tetaRad) * factor;
                        y5Neg = y4Neg + endThrow * Math.Sin(tetaRad) * factor;
                    }
                }

                x5List.Add(x5);
                y5List.Add(y5);
                x5NegList.Add(x5Neg);
                y5NegList.Add(y5Neg);
                #endregion
            }

            for (int i = 0; i < _noOfPoint; i++)
            {
                #region Maximum Vehicle Body Roll
                double xp = 0;//AF22
                double yp = 0;//AG22
                double x = 0;//AM22
                double y = 0;//AN22
                double x6 = 0;//AO22
                double y6 = 0;//AN22
                double x7 = 0;//AQ22
                double y7 = 0;//AR22
                double xpNeg = 0;//AF55
                double ypNeg = 0;//AG55
                double xNeg = 0;//AM55
                double yNeg = 0;//AN55
                double x6Neg = 0;//AO55
                double y6Neg = 0;//AN55
                double x7Neg = 0;//AQ55
                double y7Neg = 0;//AR55

                if (i < _noOfPoint / 2)
                {
                    xp = x5List[i] - x5List[0];
                    yp = y5List[i] - y5List[0];
                    xpNeg = x5NegList[i] - x5List[0];
                    ypNeg = y5NegList[i] - y5List[0];
                }
                else
                {
                    xp = x5List[i] - x5List[_HorizontalData.Count - 1];
                    yp = y5List[i] - y5List[_HorizontalData.Count - 1];
                    xpNeg = x5NegList[i] - x5List[_HorizontalData.Count - 1];
                    ypNeg = y5NegList[i] - y5List[_HorizontalData.Count - 1];
                }

                xpList.Add(xp);
                ypList.Add(yp);
                xpNegList.Add(xpNeg);
                ypNegList.Add(ypNeg);

                //AH23
                double R = Math.Sqrt(Math.Pow(xp, 2) + Math.Pow(yp, 2));
                //AH56
                double RNeg = Math.Sqrt(Math.Pow(xpNeg, 2) + Math.Pow(ypNeg, 2));
                RList.Add(R);
                RNegList.Add(RNeg);

                //AI23
                double tetaRad = Math.Asin(yp / R);
                //AI56
                double tetaNegRad = Math.Asin(ypNeg / RNeg);
                //AU23
                double tetaDeg = DataConvert.RadToDeg(tetaRad);
                //AU56
                double tetaNegDeg = DataConvert.RadToDeg(tetaNegRad);

                bodyRollTetaDegList.Add(tetaDeg);
                bodyRollTetaRadList.Add(tetaRad);
                bodyRollTetaDegNegList.Add(tetaNegDeg);
                bodyRollTetaRadNegList.Add(tetaNegRad);

                //AK23
                double tetaFirstDeg = tetaDeg - _MaxBodyRollDeg;
                //AK56
                double tetaFirstNegDeg = tetaNegDeg - _MaxBodyRollDeg;
                //AL23
                double tetaFirstRad = DataConvert.DegToRad(tetaFirstDeg);
                //AL56
                double tetaFirstNegRad = DataConvert.DegToRad(tetaFirstNegDeg);

                bodyRollTetaFirstDegList.Add(tetaFirstDeg);
                bodyRollTetaFirstRadList.Add(tetaFirstRad);
                bodyRollTetaFirstDegNegList.Add(tetaFirstNegDeg);
                bodyRollTetaFirstRadNegList.Add(tetaFirstNegRad);

                if (xp < 0)
                    x = -R * Math.Cos(tetaFirstRad);
                else
                    x = R * Math.Cos(tetaFirstRad);
                y = R * Math.Sin(tetaFirstRad);

                if (xpNeg < 0)
                    xNeg = -RNeg * Math.Cos(tetaFirstNegRad);
                else
                    xNeg = RNeg * Math.Cos(tetaFirstNegRad);
                yNeg = RNeg * Math.Sin(tetaFirstNegRad);

                bodyRollx.Add(x);
                bodyRolly.Add(y);
                bodyRollNegx.Add(xNeg);
                bodyRollNegy.Add(yNeg);

                if (i < _noOfPoint / 2)
                {
                    x6 = x5List[0] + (x5List[i] - x5List[0]) * Math.Cos(_MaxBodyRollRad) - (y5List[i] - y5List[0]) * Math.Sin(_MaxBodyRollRad);
                    y6 = y5List[0] + (x5List[i] - x5List[0]) * Math.Sin(_MaxBodyRollRad) + (y5List[i] - y5List[0]) * Math.Cos(_MaxBodyRollRad);
                    x6Neg = x5NegList[0] + (x5NegList[i] - x5NegList[0]) * Math.Cos(_MaxBodyRollRad) - (y5NegList[i] - y5NegList[0]) * Math.Sin(_MaxBodyRollRad);
                    y6Neg = y5NegList[0] + (x5NegList[i] - x5NegList[0]) * Math.Sin(_MaxBodyRollRad) + (y5NegList[i] - y5NegList[0]) * Math.Cos(_MaxBodyRollRad);
                }
                else
                {
                    x6 = x5List[x5List.Count - 1] + (x5List[i] - x5List[x5List.Count - 1]) * Math.Cos(-_MaxBodyRollRad) - (y5List[i] - y5List[y5List.Count - 1]) * Math.Sin(-_MaxBodyRollRad);
                    y6 = y5List[y5List.Count - 1] + (x5List[i] - x5List[x5List.Count - 1]) * Math.Sin(-_MaxBodyRollRad) + (y5List[i] - y5List[y5List.Count - 1]) * Math.Cos(-_MaxBodyRollRad);
                    x6Neg = x5NegList[x5NegList.Count - 1] + (x5NegList[i] - x5NegList[x5NegList.Count - 1]) * Math.Cos(-_MaxBodyRollRad) - (y5NegList[i] - y5NegList[y5NegList.Count - 1]) * Math.Sin(-_MaxBodyRollRad);
                    y6Neg = y5NegList[y5NegList.Count - 1] + (x5NegList[i] - x5NegList[x5NegList.Count - 1]) * Math.Sin(-_MaxBodyRollRad) + (y5NegList[i] - y5NegList[y5NegList.Count - 1]) * Math.Cos(-_MaxBodyRollRad);
                }

                KE1_x.Add(x6);
                KE1_y.Add(y6);

                KE3_x.Add(x6Neg);
                KE3_y.Add(y6Neg);
                #endregion

                #region Bounce
                double tetaBounceRad = 0;//AS23
                double tetaBounceDeg = 0;//AT23
                double xFirstBounce = 0;//AU23
                double yFirstBounce = 0;//AV23

                if (i < _noOfPoint / 2)
                {
                    tetaBounceRad = _tetaRad[i] - _MaxBodyRollRad;
                    tetaBounceDeg = DataConvert.RadToDeg(tetaBounceRad);
                    xFirstBounce = _Bounce * Math.Sin(tetaBounceRad);
                    yFirstBounce = _Bounce * Math.Cos(tetaBounceRad);
                }
                else
                {
                    tetaBounceDeg = 90 - _tetaDeg[i] - _MaxBodyRollDeg;
                    tetaBounceRad = DataConvert.DegToRad(tetaBounceDeg);
                    xFirstBounce = _Bounce * Math.Cos(tetaBounceRad);
                    yFirstBounce = _Bounce * Math.Sin(tetaBounceRad);
                }

                bounceTetaRadList.Add(tetaBounceRad);
                bounceTetaDegList.Add(tetaBounceDeg);
                bounceListx.Add(xFirstBounce);
                bounceListy.Add(yFirstBounce);

                x7 = x6 + xFirstBounce;//AW23
                y7 = y6 + yFirstBounce;//AX23
                x7Neg = x6Neg + xFirstBounce;//AW56
                y7Neg = y6Neg + yFirstBounce;//AX56

                KE2_x.Add(x7);
                KE2_y.Add(y7);

                KE4_x.Add(x7Neg);
                KE4_y.Add(y7Neg);
                #endregion
            }

            #region Calculate Structural Gauge
            //y coordinate
            structuralGaugeY.Add(0);
            structuralGaugeY.Add(3800);
            structuralGaugeY.Add(4670 + _Superelevation * 1.2);
            structuralGaugeY.Add(4670 + _Superelevation * 1.2);
            structuralGaugeY.Add(3800);
            structuralGaugeY.Add(0);

            // x coordinate
            if (_Superelevation == 0)
            {
                structuralGaugeX.Add(-(2060 + endThrow));
                structuralGaugeX.Add(-(2060 + endThrow));
                structuralGaugeX.Add(-(1525 + endThrow));
                structuralGaugeX.Add(1525 + endThrow);
                structuralGaugeX.Add(2060 + endThrow);
                structuralGaugeX.Add(2060 + endThrow);
            }
            else
            {
                if (curveSide == CurveSide.LEFT)
                {
                    structuralGaugeX.Add(-(2060 + endThrow + _Superelevation * 3800 / _TrackGauge));
                    structuralGaugeX.Add(-(2060 + endThrow + _Superelevation * 3800 / _TrackGauge));
                    structuralGaugeX.Add(-(1525 + endThrow + _Superelevation * (double)structuralGaugeY[2] / _TrackGauge));
                    structuralGaugeX.Add((1525 + endThrow - _Superelevation * (double)structuralGaugeY[3] / _TrackGauge));
                    structuralGaugeX.Add(2060 + endThrow - _Superelevation * 3800 / _TrackGauge);
                    structuralGaugeX.Add(2060 + endThrow - _Superelevation * 3800 / _TrackGauge);
                }
                else if (curveSide == CurveSide.RIGHT)
                {
                    structuralGaugeX.Add(-(2060 + endThrow - _Superelevation * 3800 / _TrackGauge));
                    structuralGaugeX.Add(-(2060 + endThrow - _Superelevation * 3800 / _TrackGauge));
                    structuralGaugeX.Add(-(1525 + endThrow - _Superelevation * (double)structuralGaugeY[2] / _TrackGauge));
                    structuralGaugeX.Add((1525 + endThrow + _Superelevation * (double)structuralGaugeY[3] / _TrackGauge));
                    structuralGaugeX.Add(2060 + endThrow + _Superelevation * 3800 / _TrackGauge);
                    structuralGaugeX.Add(2060 + endThrow + _Superelevation * 3800 / _TrackGauge);
                }
            }
            #endregion

            #region Output //remove the first and last item in the output list
            //Static KE
            List<double> container = new List<double>();
            container = staticKEx;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            staticKE[0] = container.ToArray();

            container = new List<double>();
            container = staticKEy;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            staticKE[1] = container.ToArray();

            //KE1
            container = new List<double>();
            container = KE1_x;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE1[0] = container.ToArray();

            container = new List<double>();
            container = KE1_y;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE1[1] = container.ToArray();

            //KE2
            container = new List<double>();
            container = KE2_x;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE2[0] = container.ToArray();

            container = new List<double>();
            container = KE2_y;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE2[1] = container.ToArray();

            //KE3
            container = new List<double>();
            container = KE3_x;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE3[0] = container.ToArray();

            container = new List<double>();
            container = KE3_y;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE3[1] = container.ToArray();

            //KE4
            container = new List<double>();
            container = KE4_x;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE4[0] = container.ToArray();

            container = new List<double>();
            container = KE4_y;
            container.RemoveAt(_noOfPoint - 1);
            container.RemoveAt(0);
            KE4[1] = container.ToArray();

            //Structural Gauge
            structuralGauge[0] = structuralGaugeX.ToArray();
            structuralGauge[1] = structuralGaugeY.ToArray();

            //warning message
            //string[][] warnings = new string[1][];
            //warnings[0] = _WarningMegs.ToArray();
            #endregion

            #region output as dictionary
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add("Static", staticKE);
            keyValuePairs.Add("KE1", KE1);
            keyValuePairs.Add("KE2", KE2);
            keyValuePairs.Add("KE3", KE3);
            keyValuePairs.Add("KE4", KE4);
            keyValuePairs.Add("Structural Gauge", structuralGauge);
            keyValuePairs.Add("Warnings", _WarningMegs.ToArray());
            #endregion

            return keyValuePairs;
        }

        #region private functions
        /// <summary>
        /// load vehicleData to memory to get Rolling stock constants
        /// </summary>
        /// <param name="vData"></param>
        private static void GetVehicleData(VehicleData vData)
        {
            _BogieCentres = vData.BogieCentres;
            _RollingStockLateralTolerance = vData.LateralTolerance;
            _VehicleLength = vData.Length;
            //_BodyOverhang = vData.BodyOverhang;
            _VehicleWidth = vData.Width;
            _MaxBodyRollDeg = vData.MaximumBodyRolldeg;
            _MaxBodyRollRad = (Math.Round((Math.PI / 180) * vData.MaximumBodyRolldeg, 4));
            _Bounce = vData.Bounce;

            _HorizontalData = new List<double>();
            _HorizontalData.Add(_HorizontalPivot);///Calculation=>General D22
            _HorizontalData.AddRange(vData.HorizontalData);
            _HorizontalData.Add(_HorizontalPivot); ///Calculation=>General D51

            _VerticalData = new List<double>();
            _VerticalData.Add(_VerticalPivot); //Calculation=>General E22
            _VerticalData.AddRange(vData.VerticalData);
            _VerticalData.Add(_VerticalPivot);//Calculation=>General E51

            _TrackGauge = vData.TrackGauge;
            _HalfHeight = (vData.VerticalData.Max() - vData.VerticalData.Min()) / 2;
        }

        /// <summary>
        /// Define the Super Tolerance Case when the Superelevation is equal to 0
        /// </summary>
        /// <returns>List of x and y</returns>
        private static List<double> GetSuperToleranceCase(double horiz, double vert, int i)
        {
            double x;
            double y;

            double x2Left10Pos = (-_TrackGauge / 2) + Math.Cos(_RotAngleToSuperTolRad) * (horiz - (-_TrackGauge / 2)) - Math.Sin(_RotAngleToSuperTolRad) * vert;
            double y2Left10Pos = Math.Sin(_RotAngleToSuperTolRad) * (horiz - (-_TrackGauge / 2)) + Math.Cos(_RotAngleToSuperTolRad) * vert;
            double x2Left10Neg = (-_TrackGauge / 2) + Math.Cos(-_RotAngleToSuperTolRad) * (horiz - (-_TrackGauge / 2)) - Math.Sin(-_RotAngleToSuperTolRad) * vert;
            double y2Left10neg = Math.Sin(-_RotAngleToSuperTolRad) * (horiz - (-_TrackGauge / 2)) + Math.Cos(-_RotAngleToSuperTolRad) * vert;

            double x2Right10Pos = (_TrackGauge / 2) + Math.Cos(-_RotAngleToSuperTolRad) * (horiz - (_TrackGauge / 2)) - Math.Sin(-_RotAngleToSuperTolRad) * vert;
            double y2Right10Pos = Math.Sin(-_RotAngleToSuperTolRad) * (horiz - (_TrackGauge / 2)) + Math.Cos(-_RotAngleToSuperTolRad) * vert;
            double x2Right10Neg = (_TrackGauge / 2) + Math.Cos(_RotAngleToSuperTolRad) * (horiz - (_TrackGauge / 2)) - Math.Sin(_RotAngleToSuperTolRad) * vert;
            double y2Right10neg = Math.Sin(_RotAngleToSuperTolRad) * (horiz - (_TrackGauge / 2)) + Math.Cos(_RotAngleToSuperTolRad) * vert;

            if (horiz != 0)
            {
                if (horiz > 0 && vert >= _HalfHeight)
                    x = x2Right10Pos;
                else
                {
                    if (horiz > 0 && vert < _HalfHeight)
                        x = x2Left10Neg;
                    else
                    {
                        if (horiz < 0 && vert >= _HalfHeight)
                            x = x2Left10Pos;
                        else
                            x = x2Right10Neg;
                    }
                }
            }
            else
            {
                if (i < _noOfPoint/2)
                {
                    if (vert >= _HalfHeight)
                        x = x2Left10Pos;
                    else
                        x = x2Right10Neg;
                }
                else
                {
                    if (vert >= _HalfHeight)
                        x = x2Right10Pos;
                    else
                        x = x2Left10Neg;
                }
            }

            if (horiz >= 0 && vert >= _HalfHeight)
            {
                y = y2Right10Pos;
            }
            else
            {
                if (horiz >= 0 && vert < _HalfHeight)
                {
                    y = y2Left10neg;
                }
                else
                {
                    if (horiz < 0 && vert > _HalfHeight)
                        y = y2Left10Pos;
                    else
                        y = y2Right10neg;
                }
            }

            return new List<double> { x, y };

        }
        #endregion
    }
}
