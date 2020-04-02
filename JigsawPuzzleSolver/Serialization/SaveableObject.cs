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
using System.Collections;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Class containing extension methods for object serialization and deserialization using the data contract serializer. Can be used to save to file, string or stream and load.
    /// </summary>
    [DataContract]
    public static class SaveHelper
    {
        /// <summary>
        /// Save the obj to an XML file.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to save</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="xmlFilepath">Filepath of the XML output file</param>
        /// <param name="compress">Compress the file using GZip or not</param>
        public static void SaveObjectToFile<Tobj>(this Tobj obj, string xmlFilepath, bool compress)
        {
            FileStream fileStream = new FileStream(xmlFilepath, FileMode.Create);
            SaveObjectToStream<Tobj>(obj, fileStream, compress, true);
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            
        /// <summary>
        /// Save the obj to an XML string.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to save</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="compress">Compress the file using GZip or not</param>
        /// <returns>String representing the saved object</returns>
        public static string SaveObjectToString<Tobj>(this Tobj obj, bool compress)
        {
            MemoryStream memoryStream = new MemoryStream();
            SaveObjectToStream<Tobj>(obj, memoryStream, compress, false);
            memoryStream.Position = 0;
            StreamReader reader = new StreamReader(memoryStream);
            string content = reader.ReadToEnd();
            memoryStream?.Close();
            return content;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Save the obj to an stream.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to save</typeparam>
        /// <param name="obj">Object to save</param>
        /// <param name="stream">Stream to save the object to</param>
        /// <param name="compress">Compress the file using GZip or not</param>
        /// <param name="closeStreamAfterWriting">Close the stream after writing or not</param>
        public static void SaveObjectToStream<Tobj>(this Tobj obj, Stream stream, bool compress, bool closeStreamAfterWriting)
        {
            if (obj == null) { return; }
            
            GZipStream compressedStream = null;
            XmlWriter writer = null;

            try
            {
                List<Type> knownTypes = new List<Type>();

                Type objType = typeof(Tobj);
                if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    knownTypes.Add(objType);
                    objType = objType.GetGenericArguments()[0];
                }
                else if(objType.IsArray)
                {
                    knownTypes.Add(objType);
                    objType = objType.GetElementType();
                }

                knownTypes.AddRange(objType.GetProperties().Select(p => p.PropertyType));
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                List<Type> derivedTypes = executingAssembly.GetTypes().Where(t => objType.IsAssignableFrom(t)).ToList();
                knownTypes.AddRange(derivedTypes);
                
                if (compress) { compressedStream = new GZipStream(stream, CompressionMode.Compress, false); }
                writer = XmlWriter.Create(compress ? (Stream)compressedStream : stream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(Tobj), knownTypes);
                serializer.WriteObject(writer, obj);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
                compressedStream?.Close();
                if (closeStreamAfterWriting) { stream?.Close(); }
            }
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// Load the object from an XML file.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to load</typeparam>
        /// <param name="xmlFilepath">Filepath of the XML input file</param>
        /// <param name="decompress">Decompress the file using GZip or not</param>
        /// <returns>Loaded object</returns>
        public static Tobj LoadObjectFromFile<Tobj>(string xmlFilepath, bool decompress)
        {
            if (string.IsNullOrEmpty(xmlFilepath) || !File.Exists(xmlFilepath)) { return default(Tobj); }
            FileStream fileStream = new FileStream(xmlFilepath, FileMode.Open);
            return LoadObjectFromStream<Tobj>(fileStream, decompress, true);
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Load the object from an XML string.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to load</typeparam>
        /// <param name="objString">String to load the saved object from</param>
        /// <param name="decompress">Decompress the file using GZip or not</param>
        /// <returns>Loaded object</returns>
        public static Tobj LoadObjectFromString<Tobj>(string objString, bool decompress)
        {
            if (string.IsNullOrEmpty(objString)) { return default(Tobj); }
            MemoryStream memoryStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memoryStream);
            writer.Write(objString);
            writer.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            Tobj obj = LoadObjectFromStream<Tobj>(memoryStream, decompress, false);
            memoryStream?.Close();
            return obj;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Load the object from an stream.
        /// </summary>
        /// <typeparam name="Tobj">Type of the object to load</typeparam>
        /// <param name="stream">Stream to load the saved object from</param>
        /// <param name="decompress">Decompress the file using GZip or not</param>
        /// <param name="closeStreamAfterReading">Close the stream after reading or not</param>
        /// <returns>Loaded object</returns>
        public static Tobj LoadObjectFromStream<Tobj>(Stream stream, bool decompress, bool closeStreamAfterReading)
        {
            if (stream == null || stream.Length == 0) { return default(Tobj); }

            Tobj objectOut = default(Tobj);
            GZipStream decompressedStream = null;
            XmlReader reader = null;

            try
            {
                List<Type> knownTypes = new List<Type>();

                Type objType = typeof(Tobj);
                if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    knownTypes.Add(objType);
                    objType = objType.GetGenericArguments()[0];
                }
                else if (objType.IsArray)
                {
                    knownTypes.Add(objType);
                    objType = objType.GetElementType();
                }

                knownTypes.AddRange(objType.GetProperties().Select(p => p.PropertyType));
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                List<Type> derivedTypes = executingAssembly.GetTypes().Where(t => objType.IsAssignableFrom(t)).ToList();
                knownTypes.AddRange(derivedTypes);

                if (decompress) { decompressedStream = new GZipStream(stream, CompressionMode.Decompress, false); }
                reader = XmlReader.Create(decompress ? decompressedStream : stream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(Tobj), knownTypes);
                objectOut = (Tobj)serializer.ReadObject(reader);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                reader?.Close();
                decompressedStream?.Close();
                if (closeStreamAfterReading) { stream?.Close(); }
            }

            return objectOut;
        }
    }

    //##############################################################################################################################################################################################
    //##############################################################################################################################################################################################
    //##############################################################################################################################################################################################

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
            SaveHelper.SaveObjectToFile(this, xmlFilepath, compress);
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
            return SaveHelper.LoadObjectFromFile<Tchild>(xmlFilepath, decompress);
        }
    }
}
