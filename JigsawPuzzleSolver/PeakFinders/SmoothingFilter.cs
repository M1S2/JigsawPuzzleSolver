using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    //see: https://www.robosoup.com/2014/01/cleaning-noisy-time-series-data-low-pass-filter-c.html
    public static class SmoothingFilter
    {
        /// <summary>
        /// Smooth the inputData
        /// </summary>
        /// <param name="inputData">Noisy input data</param>
        /// <param name="range">Number of data points each side to sample.</param>
        /// <param name="decay">[0.0 – 1.0] How slowly to decay from raw value.</param>
        /// <returns>Smoothey inputData</returns>
        public static List<double> SmoothData(List<double> inputData, int range, double decay)
        {
            double[] clean = new double[inputData.Count];
            double[] coefficients = Coefficients(range, decay);
            // Calculate divisor value.
            double divisor = 0;
            for (int i = -range; i <= range; i++)
            {
                divisor += coefficients[Math.Abs(i)];
            }
            // Clean main data.
            for (int i = range; i < clean.Length - range; i++)
            {
                double temp = 0;
                for (int j = -range; j <= range; j++)
                {
                    temp += inputData[i + j] * coefficients[Math.Abs(j)];
                }
                clean[i] = temp / divisor;
            }
            // Calculate leading and trailing slopes.
            double leadSum = 0;
            double trailSum = 0;
            int leadRef = range;
            int trailRef = clean.Length - range - 1;
            for (int i = 1; i <= range; i++)
            {
                leadSum += (clean[leadRef] - clean[leadRef + i]) / i;
                trailSum += (clean[trailRef] - clean[trailRef - i]) / i;
            }
            double leadSlope = leadSum / range;
            double trailSlope = trailSum / range;
            // Clean edges.
            for (int i = 1; i <= range; i++)
            {
                clean[leadRef - i] = clean[leadRef] + leadSlope * i;
                clean[trailRef + i] = clean[trailRef] + trailSlope * i;
            }
            return clean.ToList();
        }

        private static double[] Coefficients(int range, double decay)
        {
            // Precalculate coefficients.
            double[] coefficients = new double[range + 1];
            for (int i = 0; i <= range; i++)
            {
                coefficients[i] = Math.Pow(decay, i);
            }
            return coefficients;
        }
    }
}
