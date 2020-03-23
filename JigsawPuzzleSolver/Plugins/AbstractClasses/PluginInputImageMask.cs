using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace JigsawPuzzleSolver.Plugins.AbstractClasses
{
    public abstract class PluginInputImageMask : Plugin
    {
        public abstract Image<Gray, byte> getMask(Image<Rgba, byte> inputImg);
    }
}
