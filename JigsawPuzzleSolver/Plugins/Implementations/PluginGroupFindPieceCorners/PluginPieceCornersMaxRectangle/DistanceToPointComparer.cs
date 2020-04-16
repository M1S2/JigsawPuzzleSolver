using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupFindPieceCorners
{
    public enum DistanceOrders { NEAREST_FIRST, FAREST_FIRST }

    /// <summary>
    /// Use this class to order a list of PointF by the distance of the points to another PointF (origin)
    /// </summary>
    public class DistanceToPointComparer : IComparer<Point>
    {
        /// <summary>
        /// Origin point to which the distances are calculated
        /// </summary>
        public PointF Origin { get; private set; }

        /// <summary>
        /// How to order the points (nearest or farest first)
        /// </summary>
        public DistanceOrders DistanceOrder { get; private set; }

        /// <summary>
        /// Create a new DistanceToPointComparer
        /// </summary>
        /// <param name="origin">Origin point to which the distances are calculated</param>
        /// <param name="distanceOrder">How to order the points (nearest or farest first)</param>
        public DistanceToPointComparer(Point origin, DistanceOrders distanceOrder)
        {
            Origin = origin;
            DistanceOrder = distanceOrder;
        }

        /// <summary>
        /// Compare the distances to the points
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns>Compare result</returns>
        public int Compare(Point p1, Point p2)
        {
            double distance1 = Utils.Distance(Origin, p1);
            double distance2 = Utils.Distance(Origin, p2);

            if (DistanceOrder == DistanceOrders.NEAREST_FIRST && distance1 > distance2) { return 1; }
            else if (DistanceOrder == DistanceOrders.NEAREST_FIRST && distance1 < distance2) { return -1; }
            else if (DistanceOrder == DistanceOrders.FAREST_FIRST && distance1 > distance2) { return -1; }
            else if (DistanceOrder == DistanceOrders.FAREST_FIRST && distance1 < distance2) { return 1; }
            else { return 0; }
        }
    }
}
