using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;

namespace JigsawPuzzleSolver
{
#warning Remove TestClass. Only for testing!!!
    public class TestClassXmlSerialization : SaveableObjectXML<TestClassXmlSerialization>
    {
        /*#region Save and Load helpers
        private SaveableObjectXML<TestClassXmlSerialization> saveObjHandle = new SaveableObjectXML<TestClassXmlSerialization>();
        public void Save(string xmlFilepath) { saveObjHandle.Save(xmlFilepath); }
        public static TestClassXmlSerialization Load(string xmlFilepath) { return SaveableObjectXML<TestClassXmlSerialization>.Load(xmlFilepath); }
        #endregion*/

        public string ABC { get; set; }
        public SubTestClassXML subClass { get; set; }

        public TestClassXmlSerialization(string abc)
        {
            ABC = abc;
            subClass = new SubTestClassXML(25);
        }

        public TestClassXmlSerialization() { }
    }

    public class SubTestClassXML
    {
        public int Num { get; set; }
        public BitmapSerializable Bmp { get; set; }
        public Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte> PieceImgColor { get; set; }

        public SubTestClassXML(int num)
        {
            Num = num;
            Bmp = new BitmapSerializable(new System.Drawing.Bitmap(100, 200));
            PieceImgColor = new Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte>(Bmp.Data);
        }

        public SubTestClassXML() { }
    }


    //##############################################################################################################################################################################################

    // https://codereview.stackexchange.com/questions/7673/serialize-c-objects-of-unknown-type-to-bytes-using-generics

    /// <summary>
    /// Use this class to save an object to XML or load an object from XML.
    /// </summary>
    /// <typeparam name="Tchild">Type of the child class</typeparam>
    public class SaveableObjectXML<Tchild>
    {
        /// <summary>
        /// Save this instance of the SaveableObject to an XML file.
        /// </summary>
        /// <param name="xmlFilepath">XML file path.</param>
        public void Save(string xmlFilepath)
        {
            SerializeObject(this, xmlFilepath);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Load a new SaveableObject from an XML file.
        /// </summary>
        /// <param name="xmlFilepath">XML file path.</param>
        /// <returns>Loaded SaveableObject</returns>
        public static Tchild Load(string xmlFilepath)
        {
            //Type callingType = MethodBase.GetCurrentMethod().DeclaringType;
            return DeSerializeObject(xmlFilepath);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="serializableObject">object to serialize</param>
        /// <param name="fileName">file path of the serialized object</param>
        /// see: https://stackoverflow.com/questions/6115721/how-to-save-restore-serializable-object-to-from-file
        private static void SerializeObject(SaveableObjectXML<Tchild> serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                List<Type> types = Assembly.GetAssembly(typeof(SaveableObjectXML<Tchild>)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SaveableObjectXML<Tchild>))).ToList();

                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(SaveableObjectXML<Tchild>), types.ToArray());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Deserializes an xml file into an object
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="fileName">file path of the xml file</param>
        /// <returns>deserialized object</returns>
        /// see: https://stackoverflow.com/questions/6115721/how-to-save-restore-serializable-object-to-from-file
        private static Tchild DeSerializeObject(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(Tchild); }

            Tchild objectOut = default(Tchild);

            try
            {
                List<Type> types = Assembly.GetAssembly(typeof(SaveableObjectXML<Tchild>)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(SaveableObjectXML<Tchild>))).ToList();

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveableObjectXML<Tchild>), types.ToArray());
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (Tchild)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }

            return objectOut;
        }
    }
}
