using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections.ObjectModel;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Class that holds image processing results that can be viewed for debugging reasons.
    /// </summary>
    public class ProcessedImagesStorage
    {
        public static Dictionary<string, Bitmap> ImageList { get; private set; }
        public static ObservableCollection<string> ImageDescriptions { get; private set; }

        static ProcessedImagesStorage()
        {
            ImageList = new Dictionary<string, Bitmap>();
            ImageDescriptions = new ObservableCollection<string>();
        }

        /// <summary>
        /// Add an image with the specific description to the ImageList
        /// </summary>
        /// <param name="description">Image description</param>
        /// <param name="image">Image to store</param>
        public static void AddImage(string description, Bitmap image)
        {
            if(ImageList.ContainsKey(description)) { return; }
            ImageList.Add(description, image);
            ImageDescriptions.Add(description);
        }

        /// <summary>
        /// Get an stored image by the description
        /// </summary>
        /// <param name="description">Description of the image to get</param>
        /// <returns>Stored image with the given description</returns>
        public static Bitmap GetImage(string description)
        {
            if(!ImageList.ContainsKey(description)) { return null; }
            return ImageList[description];
        }

    }
}
