using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Xml;
using System.Reflection;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Use this class to save an object to XML or load an object from XML using the data contract serializer.
    /// </summary>
    /// <typeparam name="Tchild">Type of the child class</typeparam>
    [DataContract]
    public class SaveableObject<Tchild>
    {
        /// <summary>
        /// Save this instance of the SaveableObject to an XML file.
        /// </summary>
        /// <param name="xmlFilepath">XML file path.</param>
        /// <param name="compress">use GZip for compression or not</param>
        public void Save(string xmlFilepath, bool compress)
        {
            SaveableObject<Tchild> serializableObject = this;
            if (serializableObject == null) { return; }

            FileStream fileStream = null;
            GZipStream compressedStream = null;
            XmlWriter writer = null;

            try
            {
                List<Type> knownTypes = typeof(Tchild).GetProperties().Select(p => p.PropertyType).ToList();
                knownTypes.Add(typeof(Tchild));

                fileStream = new FileStream(xmlFilepath, FileMode.Create);
                if (compress) { compressedStream = new GZipStream(fileStream, CompressionMode.Compress, false); }
                writer = XmlWriter.Create(compress ? (Stream)compressedStream : fileStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(SaveableObject<Tchild>), knownTypes);
                serializer.WriteObject(writer, serializableObject);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
                compressedStream?.Close();
                fileStream?.Close();
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Load a new SaveableObject from an XML file.
        /// </summary>
        /// <param name="xmlFilepath">XML file path.</param>
        /// <param name="decompress">use GZip for decompression or not</param>
        /// <returns>Loaded SaveableObject</returns>
        public static Tchild Load(string xmlFilepath, bool decompress)
        {
            if (string.IsNullOrEmpty(xmlFilepath)) { return default(Tchild); }

            Tchild objectOut = default(Tchild);
            Stream fileStream = null;
            GZipStream decompressedStream = null;
            XmlReader reader = null;

            try
            {
                List<Type> knownTypes = typeof(Tchild).GetProperties().Select(p => p.PropertyType).ToList();
                knownTypes.Add(typeof(Tchild));

                fileStream = new FileStream(xmlFilepath, FileMode.Open);
                if (decompress) { decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress, false); }
                reader = XmlReader.Create(decompress ? decompressedStream : fileStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(SaveableObject<Tchild>), knownTypes);
                objectOut = (Tchild)serializer.ReadObject(reader);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                reader?.Close();
                decompressedStream?.Close();
                fileStream?.Close();
            }

            return objectOut;
        }
    }
}
