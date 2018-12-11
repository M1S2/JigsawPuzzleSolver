using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace JigsawPuzzleSolver
{
    public class Forest
    {
        public Matrix<int> locations { get; set; }
        public Matrix<int> rotations { get; set; }
        public int representative { get; set; }
        public int id { get; set; }
    };
}
