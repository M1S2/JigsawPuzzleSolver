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

        //public static List<int> FindPeaksDerivate(List<double> input, IProgress<LogBox.LogEvent> _logHandle, string PieceID)
        //{
        //    List<double> derivate = Utils.DerivativeForward(input);

        //    //derivate = SmoothingFilter.SmoothData(derivate, 3, 0.4);

        //    double minPeakDeviationThreshold = 0.5;
        //    List<int> peakPositions = new List<int>();
        //    for (int i = 0; i < derivate.Count - 1; i++)
        //    {
        //        if (Math.Abs(derivate[i]) > minPeakDeviationThreshold && Math.Abs(derivate[i + 1]) > minPeakDeviationThreshold && Math.Sign(derivate[i]) * Math.Sign(derivate[i + 1]) == -1)
        //        {
        //            peakPositions.Add(1);
        //        }
        //        else { peakPositions.Add(0); }
        //    }

        //    int maxVal = (int)derivate.Max();
        //    int minVal = (int)derivate.Min();
        //    Emgu.CV.Mat derivateImg = new Emgu.CV.Mat(maxVal - minVal, derivate.Count, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
        //    for (int i = 0; i < derivate.Count - 1; i++)
        //    {
        //        Emgu.CV.CvInvoke.Line(derivateImg, new System.Drawing.Point(i, maxVal - (int)derivate[i]), new System.Drawing.Point(i + 1, maxVal - (int)derivate[i + 1]), new Emgu.CV.Structure.MCvScalar(255, 0, 0), 1, Emgu.CV.CvEnum.LineType.EightConnected);
        //    }
        //    _logHandle.Report(new LogBox.LogEventImage(PieceID + " Derivate", derivateImg.ToImage<Emgu.CV.Structure.Rgb, byte>().Bitmap));

        //    return peakPositions;
        //}

    }
}
