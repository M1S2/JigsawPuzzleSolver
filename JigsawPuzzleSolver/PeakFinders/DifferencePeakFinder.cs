using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    public static class DifferencePeakFinder
    {
        public static List<int> FindPeaks(List<double> input, int compareSize = 10, int distFromMiddle = 2, double weightFactor = 1)
        {
            List<int> peakPositions = new List<int>(input.Count);
            for (int i = 0; i < compareSize; i++) { peakPositions.Add(0); }

            bool isPeak = true;
            for (int i = compareSize; i < input.Count - compareSize; i++)
            {
                isPeak = true;
                int loopStart = Math.Min(distFromMiddle, compareSize);
                for (int j = loopStart; j <= compareSize; j++)
                {
                    if ((input[i - j] > (input[i] * Math.Pow(weightFactor, j - loopStart))) || (input[i + j] > (input[i] * Math.Pow(weightFactor, j - loopStart)))) { isPeak = false; break; }
                }

                if (isPeak) { peakPositions.Add(1); }
                else { peakPositions.Add(0); }
            }

            for (int i = 0; i < compareSize; i++) { peakPositions.Add(0); }

            return peakPositions;
        }

        //**********************************************************************************************************************************************************************************************

        public static List<int> FindPeaksCyclic(List<double> input, int compareSize = 10, int distFromMiddle = 2, double weightFactor = 1)
        {
            if(input.Count < compareSize) { return null; }

            List<double> inputExtended = new List<double>();
            inputExtended.AddRange(input.GetRange(input.Count - compareSize, compareSize));
            inputExtended.AddRange(input);
            inputExtended.AddRange(input.GetRange(0, compareSize));

            List<int> peakPositions = new List<int>();

            bool isPeak = true;
            for (int i = compareSize; i < inputExtended.Count - compareSize; i++)
            {
                isPeak = true;
                int loopStart = Math.Min(distFromMiddle, compareSize);
                for (int j = loopStart; j <= compareSize; j++)
                {
                    if ((inputExtended[i - j] > (inputExtended[i] * Math.Pow(weightFactor, j - loopStart))) || (inputExtended[i + j] > (inputExtended[i] * Math.Pow(weightFactor, j - loopStart)))) { isPeak = false; break; }
                }

                if (isPeak) { peakPositions.Add(1); }
                else { peakPositions.Add(0); }
            }

            return peakPositions;
        }
    }
}
