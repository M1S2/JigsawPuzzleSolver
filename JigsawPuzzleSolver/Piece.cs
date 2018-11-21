using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;
using Emgu.CV.Features2D;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for the piece is that there are 4 edges the edge "numbers" go from 0-3 in counter clockwise order starting from the left.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/piece.cpp
    public class Piece
    {
        private VectorOfPointF corners;
        private int piece_size;

        /// <summary>
        /// Type of the Piece (CORNER, BORDER, INNER)
        /// </summary>
        public PieceTypes PieceType { get; private set; }

        public Mat Full_color;
        public Mat Bw;
        public Edge[] Edges = new Edge[4];

        //##############################################################################################################################################################################################

        public Piece(Mat color, Mat bw, int estimated_piece_size)
        {
            Full_color = color;
            Bw = bw;
            piece_size = estimated_piece_size;
            process();
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Find the first occurence of the elements of the second vector in the first vector and return the index in the first vector.
        /// </summary>
        /// <param name="vec1">Vector that is searched for the occurence of the elements of vector 2</param>
        /// <param name="vec2">Vector with element to be searched</param>
        /// <returns>Index of the first element in vector 1 that exist in vector 2 too</returns>
        private int find_first_in(VectorOfPointF vec1, VectorOfPointF vec2)
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
        /// <returns>Indices of all elements in vector 1 that exist in vector 2 too</returns>
        private List<int> find_all_in(VectorOfPointF vec1, VectorOfPointF vec2)
        {
            List<int> places = new List<int>();
            for(int i = 0; i < vec1.Size; i++)
            {
                for (int j = 0; j < vec2.Size; j++)
                {
                    if(vec1[i].X == vec2[j].X && vec1[i].Y == vec2[j].Y)
                    {
                        places.Add(i);
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
            find_corners();
            extract_edges();
            classify();
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find the 4 strongest corners.
        /// </summary>
        /// see: http://docs.opencv.org/doc/tutorials/features2d/trackingmotion/corner_subpixeles/corner_subpixeles.html
        private void find_corners()
        {
            double minDistance = piece_size;        //How close can 2 corners be?
            int blockSize = 25;                     //How big of an area to look for the corner in.
            bool useHarrisDetector = true;
            double k = 0.04;

            double min = 0;
            double max = 1;
            int max_iterations = 100;
            bool found_all_corners = false;

            //Binary search, altering quality until exactly 4 corners are found. Usually done in 1 or 2 iterations
            while (0 < max_iterations--)
            {
                corners.Clear();
                double qualityLevel = (min + max) / 2;

#warning Test!!!
                VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
                GFTTDetector featureDetector = new GFTTDetector(100, qualityLevel, minDistance, blockSize, useHarrisDetector, k);
                featureDetector.DetectAndCompute(Bw.Clone(), null, keyPoints, corners, false);

                if (corners.Size > 4)
                {
                    min = qualityLevel;     //Found too many corners increase quality
                }
                else if (corners.Size < 4)
                {
                    max = qualityLevel;
                }
                else
                {
                    found_all_corners = true;       //found all corners
                    break;
                }
            }

            //Find the sub-pixel locations of the corners.
            Size winSize = new Size(blockSize, blockSize);
            Size zeroZone = new Size(-1, -1);
            MCvTermCriteria criteria = new MCvTermCriteria(40, 0.001);
            //cv::TermCriteria criteria = cv::TermCriteria(CV_TERMCRIT_EPS + CV_TERMCRIT_ITER, 40, 0.001);

            // Calculate the refined corner locations
            CvInvoke.CornerSubPix(Bw, corners, winSize, zeroZone, criteria);
            
            //More debug stuff, this will mark the corners with a white circle and save the image
            //    int r = 4;
            //    for( int i = 0; i < corners.size(); i++ )
            //    { circle( full_color, corners[i],(int) corners.size(), cv::Scalar(255,255,255), -1, 8, 0 ); }
            //    std::stringstream out_file_name;
            //    out_file_name << "/tmp/final/test"<<number++<<".png";
            //    cv::imwrite(out_file_name.str(), full_color);
            
            if (!found_all_corners)
            {
                System.Windows.MessageBox.Show("Failed to find correct number of corners " + corners.Size);
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Extract the contour.
        /// TODO: probably should have this passed in from the puzzle, since it already does this. It was done this way because the contours don't correspond to the correct pixel locations in this cropped version of the image.
        /// </summary>
        private void extract_edges()
        {
            VectorOfVectorOfPointF contours = new VectorOfVectorOfPointF();
            CvInvoke.FindContours(Bw.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            //assert(corners.Size == 4);
            if (contours.Size != 1)
            {
                System.Windows.MessageBox.Show("Found incorrect number of contours " + contours.Size);
                return;
            }
            VectorOfPointF contour = contours[0];
            contour = Utils.RemoveDuplicates(contour);

            VectorOfPointF new_corners = new VectorOfPointF();
            //out of all of the found corners, find the closest points in the contour, these will become the endpoints of the edges
            for (int i = 0; i < corners.Size; i++)
            {
                double best = 10000000000;
                PointF closest_point = contour[0];
                for (int j = 0; j < contour.Size; j++)
                {
                    double d = Utils.Distance(corners[i], contour[j]);
                    if (d < best)
                    {
                        best = d;
                        closest_point = contour[j];
                    }
                }
                new_corners.Push(new PointF[] { closest_point });
            }
            corners = new_corners;

            //We need the beginning of the vector to correspond to the begining of an edge.
            PointF[] contourArray = contour.ToArray();
            contourArray.Rotate(find_first_in(contour, corners));
            contour = new VectorOfPointF(contourArray);

            //assert(corners[0] != corners[1] && corners[0] != corners[2] && corners[0] != corners[3] && corners[1] != corners[2] && corners[1] != corners[3] && corners[2] != corners[3]);

            List<int> sections = find_all_in(contour, corners);

            //Make corners go in the correct order
            VectorOfPointF new_corners2 = new VectorOfPointF();
            for (int i = 0; i < 4; i++)
            {
                new_corners2.Push(new PointF[] { contour[sections[i]] });
            }
            corners = new_corners2;

            //assert(corners[1] != corners[0] && corners[0] != corners[2] && corners[0] != corners[3] && corners[1] != corners[2] && corners[1] != corners[3] && corners[2] != corners[3]);

#warning Test !!!
            Edges[0] = new Edge(contour.GetSubsetOfVector(sections[0], sections[1]));
            Edges[1] = new Edge(contour.GetSubsetOfVector(sections[1], sections[2]));
            Edges[2] = new Edge(contour.GetSubsetOfVector(sections[2], sections[3]));
            Edges[3] = new Edge(contour.GetSubsetOfVector(sections[3], contour.Size));
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
                System.Windows.MessageBox.Show("Problem, found too many outer edges for this piece.");
            }
        }

        //##############################################################################################################################################################################################

        public PointF GetCorner(int id)
        {
            return corners[id];
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// This method "rotates the corners and edges so they are in a correct order.
        /// </summary>
        /// <param name="times">Number of times to rotate</param>
        public void Rotate(int times)
        {
#warning Test !!!
            int times_to_rotate = times % 4;
            Edges.Rotate(times_to_rotate);

            PointF[] cornersArray = corners.ToArray();
            cornersArray.Rotate(times_to_rotate);
            corners = new VectorOfPointF(cornersArray);   
        }

    }
}
