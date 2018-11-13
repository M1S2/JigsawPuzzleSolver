using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Collection of pieces. Derived from list of Piece
    /// </summary>
    public class PieceCollection : List<Piece>
    {
        /// <summary>
        /// Call InitPieceEdges for each piece in the collection
        /// </summary>
        public void InitPieceEdgesForAllPieces()
        {
            foreach (Piece p in this) { p.InitPieceEdges(); }
        }

        /// <summary>
        /// Get the piece in the collection with the given ID
        /// </summary>
        /// <param name="pieceID">ID to search</param>
        /// <returns>Piece with the given ID</returns>
        public Piece this[string pieceID]
        {
            get { return this.Where(p => p.PieceID == pieceID)?.First(); }
        }

        /// <summary>
        /// Save the original images of the pieces in the collection in the given folder
        /// </summary>
        /// <param name="folderPath">folder where the images are saved</param>
        /// see: https://stackoverflow.com/questions/15862810/a-generic-error-occurred-in-gdi-in-bitmap-save-method
        public void SavePieces(string folderPath)
        {
            foreach(Piece p in this)
            {
                string filePath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(p.OriginImageName) + "__" + p.PieceID + ".jpg");
                Directory.CreateDirectory(folderPath);             
                using (MemoryStream memory = new MemoryStream())
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        p.SourceImg.ToBitmap().Save(memory, ImageFormat.Jpeg);
                        byte[] bytes = memory.ToArray();
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

    }
}
