using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Drawing;

namespace ImageGallery.LocalDriveBitmaps
{
    /// <summary>
    /// Class to save and load bitmap direct to/from local drive. Use this to avoid RAM memory overflow when working with many bitmaps.
    /// </summary>
    [DataContract]
    public class LocalDriveBitmap
    {
        /// <summary>
        /// Full file path of the image file on the local drive.
        /// </summary>
        [DataMember]
        public string LocalFilePath { get; private set; }

        /// <summary>
        /// If true, the file is deleted on destruction of this object (file and this object exist the same amount of time); if false, the file remains on the file system after destruction
        /// </summary>
        [DataMember]
        public bool DeleteFileOnDestruction { get; private set; }

        /// <summary>
        /// Property to access the Bitmap. Nothing is stored here.
        /// </summary>
        public Bitmap Bmp
        {
            get { return Load(); }
            set { Save(value); }
        }

        /// <summary>
        /// If true, the file is only loaded and saving is forbidden (image file is never overwritten); if false, the file can be overwritten
        /// </summary>
        private bool _onlyLoadFile;

        /// <summary>
        /// Construct a new LocalDriveBitmap
        /// </summary>
        /// <param name="localFilePath">Full file path of the image file on the local drive.</param>
        /// <param name="bmp">Initial bitmap to save</param>
        /// <param name="deleteFileOnDestruction">if true, the file is deleted on destruction of this object (file and this object exist the same amount of time); if false, the file remains on the file system after destruction</param>
        /// <param name="onlyLoadFile">If true, the file is only loaded and saving is forbidden (image file is never overwritten); if false, the file can be overwritten</param>
        public LocalDriveBitmap(string localFilePath, Bitmap bmp, bool deleteFileOnDestruction = false, bool onlyLoadFile = false)
        {
            LocalFilePath = localFilePath;
            DeleteFileOnDestruction = deleteFileOnDestruction;
            _onlyLoadFile = false;
            Save(bmp);
            _onlyLoadFile = onlyLoadFile;
        }

        /// <summary>
        /// Construct a new LocalDriveBitmap
        /// </summary>
        /// <param name="localFilePath">Full file path of the image file on the local drive.</param>
        /// <param name="deleteFileOnDestruction">if true, the file is deleted on destruction of this object (file and this object exist the same amount of time); if false, the file remains on the file system after destruction</param>
        /// <param name="onlyLoadFile">If true, the file is only loaded and saving is forbidden (image file is never overwritten); if false, the file can be overwritten</param>
        public LocalDriveBitmap(string localFilePath, bool deleteFileOnDestruction = false, bool onlyLoadFile = false)
        {
            LocalFilePath = localFilePath;
            DeleteFileOnDestruction = deleteFileOnDestruction;
            _onlyLoadFile = onlyLoadFile;
        }

        /// <summary>
        /// Construct a new LocalDriveBitmap from an existing one
        /// </summary>
        /// <param name="onlyLoadFile">If true, the file is only loaded and saving is forbidden (image file is never overwritten); if false, the file can be overwritten</param>
        public LocalDriveBitmap(LocalDriveBitmap localDriveBmp, bool onlyLoadFile = false)
        {
            LocalFilePath = localDriveBmp.LocalFilePath;
            DeleteFileOnDestruction = localDriveBmp.DeleteFileOnDestruction;
            _onlyLoadFile = onlyLoadFile;
        }

        /// <summary>
        /// Destructor of LocalDriveBitmap. Deletes the file if DeleteFileOnDestruction is true.
        /// </summary>
        ~LocalDriveBitmap()
        {
            if (DeleteFileOnDestruction) { DeleteFile(); }
        }

        /// <summary>
        /// Save the bitmap to the file at LocalFilePath
        /// </summary>
        /// <param name="bmp">Bitmap to save</param>
        private void Save(Bitmap bmp)
        {
            if(bmp == null || _onlyLoadFile) { return; }
            if (!Directory.Exists(Path.GetDirectoryName(LocalFilePath))) { Directory.CreateDirectory(Path.GetDirectoryName(LocalFilePath)); }

            System.Drawing.Imaging.ImageFormat imgFormat;
            switch (Path.GetExtension(LocalFilePath).ToLower())
            {
                case ".jpg": imgFormat = System.Drawing.Imaging.ImageFormat.Jpeg; break;
                case ".bmp": imgFormat = System.Drawing.Imaging.ImageFormat.Bmp; break;
                case ".tiff": imgFormat = System.Drawing.Imaging.ImageFormat.Tiff; break;
                case ".wmf": imgFormat = System.Drawing.Imaging.ImageFormat.Wmf; break;
                case ".gif": imgFormat = System.Drawing.Imaging.ImageFormat.Gif; break;
                case ".ico": imgFormat = System.Drawing.Imaging.ImageFormat.Icon; break;
                default: imgFormat = System.Drawing.Imaging.ImageFormat.Png; break;
            }

            bmp.Save(LocalFilePath, imgFormat);
        }

        /// <summary>
        /// Load the bitmap from the file at LocalFilePath
        /// </summary>
        /// <returns>Loaded Bitmap</returns>
        private Bitmap Load()
        {
            if (!File.Exists(LocalFilePath)) { return null; }

            Image image;
            using (FileStream myStream = new FileStream(LocalFilePath, FileMode.Open))
            {
                image = Image.FromStream(myStream);
                myStream.Close();
            }
            return (Bitmap)image;
        }

        /// <summary>
        /// Delete the bitmap file at LocalFilePath
        /// </summary>
        public void DeleteFile()
        {
            if (File.Exists(LocalFilePath)) { File.Delete(LocalFilePath); }
        }
    }
}
