using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;
using Emgu.CV.Features2D;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogBox.LogEvents;
using ImageGallery.LocalDriveBitmaps;
using JigsawPuzzleSolver.Plugins;
using JigsawPuzzleSolver.Plugins.AbstractClasses;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for the piece is that there are 4 edges the edge "numbers" go from 0-3 in counter clockwise order starting from the left.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/piece.cpp
    [DataContract]
    public class Piece : SaveableObject<Piece>, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. The CallerMemberName attribute that is applied to the optional propertyName parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName">Name of the property that is changed</param>
        /// see: https://docs.microsoft.com/de-de/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //##############################################################################################################################################################################################

        public static int NextPieceID { get; set; }

        static Piece()
        {
            NextPieceID = 0;
        }

        //##############################################################################################################################################################################################

        private string _pieceSourceFileName;
        /// <summary>
        /// The name of the file from where the piece was extracted.
        /// </summary>
        [DataMember]
        public string PieceSourceFileName
        {
            get { return _pieceSourceFileName; }
            private set { _pieceSourceFileName = value;  OnPropertyChanged(); }
        }

        private Point _pieceSourceFileLocation;
        /// <summary>
        /// Location of the piece in the source file in pixel.
        /// </summary>
        [DataMember]
        public Point PieceSourceFileLocation
        {
            get { return _pieceSourceFileLocation; }
            private set { _pieceSourceFileLocation = value; OnPropertyChanged(); }
        }

        private PieceTypes _pieceType;
        /// <summary>
        /// Type of the Piece (CORNER, BORDER, INNER)
        /// </summary>
        [DataMember]
        public PieceTypes PieceType
        {
            get { return _pieceType; }
            private set { _pieceType = value; OnPropertyChanged(); }
        }

        private string _pieceID;
        /// <summary>
        /// A string that is unique for each extracted Piece.
        /// </summary>
        [DataMember]
        public string PieceID
        {
            get { return _pieceID; }
            set { _pieceID = value; OnPropertyChanged(); }
        }

        private int _pieceIndex;
        /// <summary>
        /// Index of the extracted Piece.
        /// </summary>
        [DataMember]
        public int PieceIndex
        {
            get { return _pieceIndex; }
            set { _pieceIndex = value; OnPropertyChanged(); }
        }

        private LocalDriveBitmap _pieceImgColor;
        /// <summary>
        /// Color image of the extracted piece
        /// </summary>
        [DataMember]
        public LocalDriveBitmap PieceImgColor
        {
            get { return _pieceImgColor; }
            set { _pieceImgColor = value;  OnPropertyChanged(); }
        }

        private LocalDriveBitmap _pieceImgBw;
        /// <summary>
        /// Black white image of the extracted piece
        /// </summary>
        [DataMember]
        public LocalDriveBitmap PieceImgBw
        {
            get { return _pieceImgBw; }
            set { _pieceImgBw = value; OnPropertyChanged(); }
        }

        private int _solutionRotation;
        /// <summary>
        /// Rotation of the piece in the solution image
        /// </summary>
        [DataMember]
        public int SolutionRotation
        {
            get { return _solutionRotation; }
            set { _solutionRotation = value; OnPropertyChanged(); }
        }

        private Point _solutionLocation;
        /// <summary>
        /// Location of the piece in the solution image (not pixel coordinates, but row and column index)
        /// </summary>
        [DataMember]
        public Point SolutionLocation
        {
            get { return _solutionLocation; }
            set { _solutionLocation = value; OnPropertyChanged(); }
        }

        private int _solutionID;
        /// <summary>
        /// Id of the solution the piece belongs to (there are more solutions if not all pieces fit together and there are multiple groups of pieces)
        /// </summary>
        [DataMember]
        public int SolutionID
        {
            get { return _solutionID; }
            set { _solutionID = value; OnPropertyChanged(); }
        }

        private Size _pieceSize;
        /// <summary>
        /// Size of the piece in pixel
        /// </summary>
        [DataMember]
        public Size PieceSize
        {
            get { return _pieceSize; }
            set { _pieceSize = value; OnPropertyChanged(); }
        }

        [DataMember]
        public Edge[] Edges = new Edge[4];
        
        private VectorOfPoint corners = new VectorOfPoint();
        private IProgress<LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        //##############################################################################################################################################################################################

        public Piece(Image<Rgba, byte> color, Image<Gray, byte> bw, string pieceSourceFileName, Point pieceSourceFileLocation, IProgress<LogEvent> logHandle, CancellationToken cancelToken)
        {
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            PieceID = "Piece#" + NextPieceID.ToString();
            PieceIndex = NextPieceID;
            NextPieceID++;
            
            PieceImgColor = new LocalDriveBitmap(System.IO.Path.GetDirectoryName(pieceSourceFileName) + @"\Results\" + PieceID + "_Color.png", (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults ? color.Bitmap : color.LimitImageSize(200, 200).Bitmap));
            PieceImgBw = new LocalDriveBitmap(System.IO.Path.GetDirectoryName(pieceSourceFileName) + @"\Results\" + PieceID + "_Bw.png", bw.Bitmap);
            PieceSourceFileName = pieceSourceFileName;
            PieceSourceFileLocation = pieceSourceFileLocation;
            PieceSize = bw.Bitmap.Size;

            
            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                _logHandle.Report(new LogEventImage(PieceID + " Color", color.Bitmap));
                _logHandle.Report(new LogEventImage(PieceID + " Bw", bw.Bitmap));
            }

            process();
        }

        public Piece()
        { }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Find the first occurence of the elements of the second vector in the first vector and return the index in the first vector.
        /// </summary>
        /// <param name="vec1">Vector that is searched for the occurence of the elements of vector 2</param>
        /// <param name="vec2">Vector with element to be searched</param>
        /// <returns>Index of the first element in vector 1 that exist in vector 2 too</returns>
        private int find_first_in(VectorOfPoint vec1, VectorOfPoint vec2)
        {
            List<int> all_occurences = find_all_in(vec1, vec2);
            return (all_occurences?.First()).Value;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find all occurences of the elements of the second vector in the first vector and return the indices in the first vector.
        /// </summary>
        /// <param name="vec1">Vector that is searched for the occurences of the elements of vector 2</param>
        /// <param name="vec2">Vector with element to be searched</param>
        /// <returns>Indices of all elements in vector 1 that exist in vector 2 too (ordered by occurence in vec2)</returns>
        private List<int> find_all_in(VectorOfPoint vec1, VectorOfPoint vec2)
        {
            List<int> places = new List<int>();
            for (int i2 = 0; i2 < vec2.Size; i2++)
            {
                for (int i1 = 0; i1 < vec1.Size; i1++)
                {
                    if (vec1[i1].X == vec2[i2].X && vec1[i1].Y == vec2[i2].Y)
                    {
                        places.Add(i1);
                    }
                }
            }

            return places;
        } 

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find corners, extract the edges and classify the piece
        /// </summary>
        private void process()
        {
            Bitmap bwImg = PieceImgBw.Bmp;
            Bitmap colorImg = PieceImgColor.Bmp;

            corners.Clear();
            PluginGroupFindPieceCorners pluginFindPieceCorners = PluginFactory.GetEnabledPluginsOfGroupType<PluginGroupFindPieceCorners>().FirstOrDefault();
            corners.Push(pluginFindPieceCorners?.FindCorners(PieceID, bwImg, colorImg).ToArray());

            bwImg.Dispose();
            colorImg.Dispose();

            extract_edges();
            classify();
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Extract the contour.
        /// TODO: probably should have this passed in from the puzzle, since it already does this. It was done this way because the contours don't correspond to the correct pixel locations in this cropped version of the image.
        /// </summary>
        private void extract_edges()
        {
            Bitmap bwImg = PieceImgBw.Bmp;
            _logHandle.Report(new LogEventInfo(PieceID + " Extracting edges"));

            if (corners.Size != 4) { return; }

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(new Image<Gray, byte>(bwImg), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            bwImg.Dispose();
            if (contours.Size == 0)
            {
                _logHandle.Report(new LogEventError(PieceID + " No contours found."));
                return;
            }

            int indexLargestContour = 0;                // Find the largest contour
            double largestContourArea = 0;
            for(int i = 0; i < contours.Size; i++)
            {
                double contourAreaTmp = CvInvoke.ContourArea(contours[i]);
                if(contourAreaTmp > largestContourArea) { largestContourArea = contourAreaTmp; indexLargestContour = i; }
            }

            VectorOfPoint contour = contours[indexLargestContour];

            VectorOfPoint new_corners = new VectorOfPoint();
            for (int i = 0; i < corners.Size; i++)      //out of all of the found corners, find the closest points in the contour, these will become the endpoints of the edges
            {
                double best = 10000000000;
                Point closest_point = contour[0];
                for (int j = 0; j < contour.Size; j++)
                {
                    double d = Utils.Distance(corners[i], contour[j]);
                    if (d < best)
                    {
                        best = d;
                        closest_point = contour[j];
                    }
                }
                new_corners.Push(closest_point);
            }
            corners = new_corners;

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                Bitmap colorImg = PieceImgColor.Bmp;
                Image<Rgb, byte> edge_img = new Image<Rgb, byte>(colorImg);
                for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(edge_img, Point.Round(corners[i]), 2, new MCvScalar(255, 0, 0), 1); }
                _logHandle.Report(new LogEventImage(PieceID + " New corners", edge_img.Bitmap));
                colorImg.Dispose();
                edge_img.Dispose();
            }

            List<int> sections = find_all_in(contour, corners);

            //Make corners go in the correct order
            Point[] new_corners2 = new Point[4];
            int cornerIndexUpperLeft = -1;
            double cornerDistUpperLeft = double.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                new_corners2[i] = contour[sections[i]];
                double cornerDist = Utils.DistanceToOrigin(contour[sections[i]]);
                if(cornerDist < cornerDistUpperLeft)
                {
                    cornerDistUpperLeft = cornerDist;
                    cornerIndexUpperLeft = i;
                }
            }
            new_corners2.Rotate(-cornerIndexUpperLeft);
            corners.Push(new_corners2);
            sections.Rotate(-cornerIndexUpperLeft);

            Edges[0] = new Edge(PieceID, 0, PieceImgColor, contour.GetSubsetOfVector(sections[0], sections[1]), _logHandle, _cancelToken);
            Edges[1] = new Edge(PieceID, 1, PieceImgColor, contour.GetSubsetOfVector(sections[1], sections[2]), _logHandle, _cancelToken);
            Edges[2] = new Edge(PieceID, 2, PieceImgColor, contour.GetSubsetOfVector(sections[2], sections[3]), _logHandle, _cancelToken);
            Edges[3] = new Edge(PieceID, 3, PieceImgColor, contour.GetSubsetOfVector(sections[3], sections[0]), _logHandle, _cancelToken);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Classify the type of piece
        /// </summary>
        private void classify()
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if(Edges[i] == null) { return; }
                if (Edges[i].EdgeType == EdgeTypes.LINE) count++;
            }
            if (count == 0)
            {
                PieceType = PieceTypes.INNER;
            }
            else if (count == 1)
            {
                PieceType = PieceTypes.BORDER;
            }
            else if (count == 2)
            {
                PieceType = PieceTypes.CORNER;
            }
            else
            {
                _logHandle.Report(new LogEventError(PieceID + " Found too many edges for this piece. " + count.ToString() + " found."));
            }

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                _logHandle.Report(new LogEventInfo(PieceID + " Type " + PieceType.ToString()));
            }
        }

        //##############################################################################################################################################################################################

        public PointF GetCorner(int id)
        {
            if(corners.Size == 0) { return new PointF(0, 0); }
            return corners[id];
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// This method rotates the corners and edges so they are in a correct order.
        /// </summary>
        /// <param name="times">Number of times to rotate</param>
        public void Rotate(int times)
        {
            int times_to_rotate = times % 4;
            Edges.Rotate(times_to_rotate);

            Point[] cornersArray = corners.ToArray();
            cornersArray.Rotate(times_to_rotate);
            corners = new VectorOfPoint(cornersArray);

            Bitmap colorImg = PieceImgColor.Bmp, bwImg = PieceImgBw.Bmp;
            Image<Rgb, byte> cvImgColor = new Image<Rgb, byte>(colorImg);
            Image<Gray, byte> cvImgBw = new Image<Gray, byte>(bwImg);

            colorImg.Dispose();
            bwImg.Dispose();
            PieceImgColor.DeleteFile();
            PieceImgBw.DeleteFile();

            colorImg = cvImgColor.Rotate(times_to_rotate * 90, new Rgb(255, 255, 255), false).Bitmap;
            bwImg = cvImgBw.Rotate(times_to_rotate * 90, new Gray(0), false).Bitmap;
            
            PieceImgColor.Bmp = colorImg;
            PieceImgBw.Bmp = bwImg;

            colorImg.Dispose();
            bwImg.Dispose();
            cvImgColor.Dispose();
            cvImgBw.Dispose();
        }

    }
}
