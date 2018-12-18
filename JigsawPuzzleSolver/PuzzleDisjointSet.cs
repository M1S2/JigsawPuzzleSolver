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

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Class that is used to combine the pieces of the puzzle.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/PuzzleDisjointSet.cpp
    public class PuzzleDisjointSet
    {
        public int SetCount { get; set; }          //A count of how many sets are left.

        private List<Forest> sets = new List<Forest>();

        //##############################################################################################################################################################################################

        public PuzzleDisjointSet(int number)
        {
            SetCount = 0;
            for (int i = 0; i < number; i++)
            {
                make_set(i);
            }
        }

        //##############################################################################################################################################################################################

        private void rotate_ccw(int id, int times)
        {
            int direction = times % 4;

            for (int i = 0; i < direction; i++)
            {
                sets[id].locations = Utils.RotateMatrixCounterClockwise(sets[id].locations);
                sets[id].rotations = Utils.RotateMatrixCounterClockwise(sets[id].rotations);
            }
            sets[id].rotations += direction;

            //If there is no piece at the location, the rotation needs to be set back to zero
            for (int i = 0; i < sets[id].locations.Size.Width; i++)
            {
                for (int j = 0; j < sets[id].locations.Size.Height; j++)
                {
                    if (sets[id].locations[j, i] == -1) { sets[id].rotations[j, i] = 0; }
                }
            }

            //basically rotations%4 (opencv does not have an operator to do this). Luckly the last 2 bits are all that is needed
            CvInvoke.BitwiseAnd(sets[id].rotations, new ScalarArray(3), sets[id].rotations);
            return;
        }

        //**********************************************************************************************************************************************************************************************

        private void make_set(int new_id)
        {
            Forest f = new Forest();
            f.id = new_id;
            f.representative = -1;
            f.locations = new Matrix<int>(1, 1);
            f.locations.SetValue(new_id);
            f.rotations = new Matrix<int>(1, 1);
            f.rotations.SetValue(0);
            sets.Add(f);
            SetCount++;
        }

        //**********************************************************************************************************************************************************************************************

        private Point find_location(Matrix<int> matrix, int number)
        {
            for (int i = 0; i < matrix.Size.Width; i++)
            {
                for (int j = 0; j < matrix.Size.Height; j++)
                {
                    if (matrix[j, i] == number) { return new Point(i, j); }
                }
            }
            return new Point(0, 0);
        }

        //##############################################################################################################################################################################################

        public bool JoinSets(int a, int b, int how_a, int how_b)
        {
            int rep_a = Find(a);
            int rep_b = Find(b);
            if (rep_a == rep_b) return false;   //Already in same set...
            
            //    std::cout << std::endl << sets[rep_a].rotations << std::endl << sets[rep_b].rotations << std::endl;

            //We need A to have its adjoining edge to be to the right, position 2 meaning if its rotation was 0 it would need to be rotated by 2
            Point loc_of_a = find_location(sets[rep_a].locations, a);
            int rot_a = sets[rep_a].rotations[loc_of_a.Y, loc_of_a.X];
            int to_rot_a = (6 - how_a - rot_a) % 4;
            rotate_ccw(rep_a, to_rot_a);

            //We need B to have its adjoinign edge to the left, position 0 if its position was 0 
            Point loc_of_b = find_location(sets[rep_b].locations, b);
            int rot_b = sets[rep_b].rotations[loc_of_b.Y, loc_of_b.X];
            int to_rot_b = (8 - rot_b - how_b) % 4;
            rotate_ccw(rep_b, to_rot_b);
            
            //figure out the size of the new Mats
            loc_of_a = find_location(sets[rep_a].locations, a);
            Size size_of_a = sets[rep_a].locations.Size;
            loc_of_b = find_location(sets[rep_b].locations, b);
            Size size_of_b = sets[rep_b].locations.Size;

            int width = Math.Max(size_of_a.Width, loc_of_a.X - loc_of_b.X + 1 + size_of_b.Width) - Math.Min(0, loc_of_a.X - loc_of_b.X + 1);
            int height = Math.Max(size_of_a.Height, loc_of_a.Y - loc_of_b.Y + size_of_b.Height) - Math.Min(0, loc_of_a.Y - loc_of_b.Y);

            //place old A and B into the new Mats of same size

            Matrix<int> new_a_locs = new Matrix<int>(height, width);
            new_a_locs.SetValue(-1);
            Matrix<int> new_b_locs = new Matrix<int>(height, width);
            new_b_locs.SetValue(-1);
            Matrix<int> new_a_rots = new Matrix<int>(height, width);
            new_a_rots.SetValue(0);
            Matrix<int> new_b_rots = new Matrix<int>(height, width);
            new_b_rots.SetValue(0);

            int ax_offset = Math.Abs(Math.Min(0, loc_of_a.X - loc_of_b.X + 1));
            int ay_offset = Math.Abs(Math.Min(0, loc_of_a.Y - loc_of_b.Y));

            int bx_offset = -(loc_of_b.X - (loc_of_a.X + ax_offset + 1));
            int by_offset = -(loc_of_b.Y - (loc_of_a.Y + ay_offset));
            
            sets[rep_a].locations.CopyToROI(new_a_locs, new Rectangle(ax_offset, ay_offset, size_of_a.Width, size_of_a.Height));
            sets[rep_a].rotations.CopyToROI(new_a_rots, new Rectangle(ax_offset, ay_offset, size_of_a.Width, size_of_a.Height));
            sets[rep_b].locations.CopyToROI(new_b_locs, new Rectangle(bx_offset, by_offset, size_of_b.Width, size_of_b.Height));
            sets[rep_b].rotations.CopyToROI(new_b_rots, new Rectangle(bx_offset, by_offset, size_of_b.Width, size_of_b.Height));
            
            //check for overlap while combining...
            for (int i = 0; i < new_a_locs.Size.Height; i++)
            {
                for (int j = 0; j < new_a_locs.Size.Width; j++)
                {
                    //If both have a real value for a piece, it becomes impossible, reject
                    if (new_a_locs[i, j] != -1 && new_b_locs[i, j] != -1)
                    {
                        //System.Windows.MessageBox.Show("Failed to merge because of overlap.");
                        return false;
                    }

                    if (new_a_locs[i, j] == -1 && new_b_locs[i, j] != -1)
                    {
                        new_a_locs[i, j] = new_b_locs[i, j];
                        new_a_rots[i, j] = new_b_rots[i, j];
                    }
                }
            }
            
            //Set the new representative a, to have this Mat
            sets[rep_a].locations = new_a_locs;
            sets[rep_a].rotations = new_a_rots;

            //Updating the number of sets left
            SetCount--;

            //Representative is the same idea as a disjoint set datastructure
            sets[rep_b].representative = rep_a;
            return true; //Everything seems ok if it got this far
        }

        //**********************************************************************************************************************************************************************************************

        public int Find(int a)
        {
            int rep = a;
            while (sets[rep].representative != -1)
            {
                rep = sets[rep].representative;
            }
            return rep;
        }

        //**********************************************************************************************************************************************************************************************

        public bool InSameSet(int a, int b)
        {
            return (Find(a) == Find(b));
        }

        //**********************************************************************************************************************************************************************************************

        public bool InOneSet()
        {
            return (1 == SetCount);
        }

        //**********************************************************************************************************************************************************************************************

        public Forest Get(int id)
        {
            return sets[id];
        }

        //**********************************************************************************************************************************************************************************************

        public List<Forest> GetJointSets()
        {
            return sets.Where(s => s.representative == -1).ToList();
        }
    }
}
