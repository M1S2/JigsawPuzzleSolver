using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using LogBox.LogEvents;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("PluginPieceCorners GFTT")]
    [PluginDescription("Plugin for finding piece corners using GFTT algorithm (Good feature to track)")]
    public class PluginPieceCornersGFTT : PluginGroupFindPieceCorners
    {
        private int _blockSize;
        [PluginSettingNumber(1, 1, 100)]
        [PluginSettingDescription("How big is the area to look for the corner in?")]
        public int BlockSize
        {
            get { return _blockSize; }
            set { _blockSize = value; OnPropertyChanged(); }
        }

        private int _maxIterations;
        [PluginSettingNumber(1, 1, 1000)]
        [PluginSettingDescription("Number of iterations for GFTT algorithm")]
        public int MaxIterations
        {
            get { return _maxIterations; }
            set { _maxIterations = value; OnPropertyChanged(); }
        }

        private double _harrisDetectorParameterK;
        [PluginSettingNumber(0.01, 0, 100)]
        [PluginSettingDescription("Free parameter k of the Harris detector.")]
        public double HarrisDetectorParameterK
        {
            get { return _harrisDetectorParameterK; }
            set { _harrisDetectorParameterK = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            BlockSize = 5;
            MaxIterations = 100;
            HarrisDetectorParameterK = 0.04;
        }

        /// <summary>
        /// Find the 4 strongest corners using the GFTT algorithm.
        /// </summary>
        /// <param name="pieceID">ID of the piece</param>
        /// <param name="pieceImgBw">Black white image of piece</param>
        /// <param name="pieceImgColor">Color image of piece</param>
        /// <returns>List with corner points</returns>
        /// see: http://docs.opencv.org/doc/tutorials/features2d/trackingmotion/corner_subpixeles/corner_subpixeles.html
        public override List<Point> FindCorners(string pieceID, Bitmap pieceImgBw, Bitmap pieceImgColor)
        {
            PluginFactory.LogHandle.Report(new LogEventInfo(pieceID + " Finding corners with GFTT algorithm"));

            double minDistance = PluginFactory.GetGeneralSettingsPlugin().PuzzleMinPieceSize;    //How close can 2 corners be?

            double min = 0;
            double max = 1;
            bool found_all_corners = false;

            Image<Gray, byte> bw_clone = new Image<Gray, byte>(pieceImgBw);

            List<Point> corners = new List<Point>();

            //Binary search, altering quality until exactly 4 corners are found. Usually done in 1 or 2 iterations
            while (0 < MaxIterations--)
            {
                if (PluginFactory.CancelToken.IsCancellationRequested) { PluginFactory.CancelToken.ThrowIfCancellationRequested(); }
                
                double qualityLevel = (min + max) / 2;

                VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
                GFTTDetector featureDetector = new GFTTDetector(100, qualityLevel, minDistance, BlockSize, true, HarrisDetectorParameterK);

                featureDetector.DetectRaw(bw_clone, keyPoints);

                if (keyPoints.Size > 4)
                {
                    min = qualityLevel;     //Found too many corners increase quality
                }
                else if (keyPoints.Size < 4)
                {
                    max = qualityLevel;
                }
                else
                {
                    for (int i = 0; i < keyPoints.Size; i++)
                    {
                        corners.Add(Point.Round(keyPoints[i].Point));
                    }

                    found_all_corners = true;       //found all corners
                    break;
                }
            }

            //Find the sub-pixel locations of the corners.
            //Size winSize = new Size(blockSize, blockSize);
            //Size zeroZone = new Size(-1, -1);
            //MCvTermCriteria criteria = new MCvTermCriteria(40, 0.001);

            // Calculate the refined corner locations
            //CvInvoke.CornerSubPix(bw_clone, corners, winSize, zeroZone, criteria);

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                Image<Rgb, byte> corner_img = new Image<Rgb, byte>(pieceImgColor);
                for (int i = 0; i < corners.Count; i++) { CvInvoke.Circle(corner_img, Point.Round(corners[i]), 7, new MCvScalar(255, 0, 0), -1); }
                PluginFactory.LogHandle.Report(new LogEventImage(pieceID + " Found Corners (" + corners.Count.ToString() + ")", corner_img.Bitmap));
                corner_img.Dispose();
            }

            if (!found_all_corners)
            {
                PluginFactory.LogHandle.Report(new LogEventError(pieceID + " Failed to find correct number of corners. " + corners.Count + " found."));
            }
            return corners;
        }
    }
}
