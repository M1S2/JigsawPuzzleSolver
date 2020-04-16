using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupFindPieceCorners
{
    /// <summary>
    /// Structure representing a point with polar coordinates
    /// </summary>
    public struct PolarCoordinate
    {
        /// <summary>
        /// Angle of the point in degree
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Radius of the point
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// Constructor of a new PolarCoordinate
        /// </summary>
        /// <param name="a">Angle of the point</param>
        /// <param name="r">Radius of the point</param>
        public PolarCoordinate(double a, double r)
        {
            Angle = a;
            Radius = r;
        }

        /// <summary>
        /// Override the ToString() method to display the point coordinates
        /// </summary>
        /// <returns>PolarCoordinate string</returns>
        public override string ToString()
        {
            return "Radius = " + Radius.ToString() + "; Angle = " + Angle.ToString();
        }

        /// <summary>
        /// Convert a cartesian point to polar point
        /// </summary>
        /// <param name="p">cartesian point to convert</param>
        /// <returns>polar point</returns>
        /// see: https://de.wikipedia.org/wiki/Polarkoordinaten
        public static PolarCoordinate CartesianToPolar(PointF p)
        {
            double r = Math.Sqrt((p.X * p.X) + (p.Y * p.Y));
            double a = Math.Acos(p.X / r);
            if (p.Y < 0) { a = 2 * Math.PI - a; }

            a *= (180 / Math.PI);       // Convert angle from radians to degree

            return new PolarCoordinate(a, r);
        }

        /// <summary>
        /// Convert a polar point to cartesian
        /// </summary>
        /// <param name="p">polar point to convert</param>
        /// <returns>cartesian point</returns>
        /// see: https://de.wikipedia.org/wiki/Polarkoordinaten
        public static PointF PolarToCartesian(PolarCoordinate p)
        {
            double a = p.Angle * (Math.PI / 180);       // Convert angle from degree to radians
            return new PointF((float)(p.Radius * Math.Cos(a)), (float)(p.Radius * Math.Sin(a)));
        }
    }
}
