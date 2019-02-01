using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Reflection;

namespace JigsawPuzzleSolver
{
#warning Remove TestClass. Only for testing!!!
    [DataContract]
    public class TestClass : SaveableObject<TestClass>
    {
        [DataMember]
        public string ABC { get; set; }

        [DataMember]
        public SubTestClass subClass { get; set; }

        [DataMember]
        public int Number { get; private set; }

        public TestClass(string abc)
        {
            ABC = abc;
            subClass = new SubTestClass(25);
        }

        public TestClass() { }
    }

    [DataContract]
    public class SubTestClass
    {
        [DataMember]
        public int Num { get; set; }

        //[DataMember]
        //public BitmapSerializable Bmp { get; set; }

        [DataMember]
        public System.Drawing.Bitmap BmpRaw { get; set; }
        
        //[DataMember]
        //public Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte> PieceImgColor { get; set; }
        
        [DataMember]
        private string privStr;

        [DataMember]
        public OwnImg<Emgu.CV.Structure.Rgb, byte> ownImg { get; set; }

        public SubTestClass(int num)
        {
            privStr = "xyz";
            Num = num;
            BmpRaw = new System.Drawing.Bitmap(100, 200);
            //Bmp = new BitmapSerializable(BmpRaw);
            //PieceImgColor = new Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte>(new System.Drawing.Bitmap(100, 200));
            //PieceImgColor.SerializationCompressionRatio = 9;

            ownImg = new OwnImg<Emgu.CV.Structure.Rgb, byte>(BmpRaw);
            
        }

        public SubTestClass() { }
    }

    public class OwnImg<Tc, Td> : Emgu.CV.Image<Tc, Td> where Tc : struct, Emgu.CV.IColor where Td : new()
    {
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
        }

        public OwnImg()
        { }

        public OwnImg(System.Drawing.Bitmap bmp)
        {
            Bitmap = bmp;
        }
    }

    //##############################################################################################################################################################################################

    // https://codereview.stackexchange.com/questions/7673/serialize-c-objects-of-unknown-type-to-bytes-using-generics

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
        public void Save(string xmlFilepath)
        {
            SaveableObject<Tchild> serializableObject = this;
            if (serializableObject == null) { return; }

            FileStream fileStream = null;
            XmlWriter writer = null;

            try
            {
                List<Type> knownTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(SaveableObject<>)).ToList();
                knownTypes.Add(typeof(System.Drawing.Point[]));

                fileStream = new FileStream(xmlFilepath, FileMode.Create);
                writer = XmlWriter.Create(fileStream); // XmlDictionaryWriter.CreateTextWriter(fileStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(SaveableObject<Tchild>), knownTypes);
                serializer.WriteObject(writer, serializableObject);
            }
            catch (Exception ex)
            {
                //Log exception here
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
                fileStream?.Close();
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Load a new SaveableObject from an XML file.
        /// </summary>
        /// <param name="xmlFilepath">XML file path.</param>
        /// <returns>Loaded SaveableObject</returns>
        public static Tchild Load(string xmlFilepath)
        {
            if (string.IsNullOrEmpty(xmlFilepath)) { return default(Tchild); }

            Tchild objectOut = default(Tchild);
            Stream fileStream = null;
            XmlReader reader = null;

            try
            {
                List<Type> knownTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(SaveableObject<>)).ToList();
                knownTypes.Add(typeof(System.Drawing.Point[]));

                fileStream = new FileStream(xmlFilepath, FileMode.Open);
                reader = XmlReader.Create(fileStream); //XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas() { MaxArrayLength = int.MaxValue });
                DataContractSerializer serializer = new DataContractSerializer(typeof(SaveableObject<Tchild>), knownTypes);
                objectOut = (Tchild)serializer.ReadObject(reader);
            }
            catch (Exception ex)
            {
                //Log exception here
            }
            finally
            {
                reader?.Close();
                fileStream?.Close();
            }

            return objectOut;
        }
    }
}
