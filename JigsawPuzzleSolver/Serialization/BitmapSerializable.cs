using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace JigsawPuzzleSolver
{
#warning BitmapSerializable is maybe unneccessary ???!!!

    /// <summary>
    /// Bitmap that can be serialized
    /// </summary>
    /// see: https://stackoverflow.com/questions/1907077/serialize-a-bitmap-in-c-net-to-xml
    /// see: https://stackoverflow.com/questions/279534/proper-way-to-implement-ixmlserializable
    /// see: https://stackoverflow.com/questions/7350679/convert-a-bitmap-into-a-byte-array
    public class BitmapSerializable : IXmlSerializable
    {
        public BitmapSerializable() { }
        public BitmapSerializable(Bitmap bmp) { Data = bmp; }

        public Bitmap Data { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            int byteCount = 0;
            reader.ReadStartElement("ByteCount");
            byteCount = reader.ReadContentAsInt();
            reader.ReadEndElement();

            reader.ReadStartElement("Image");
            byte[] bitmapData = new byte[byteCount];
            reader.ReadContentAsBase64(bitmapData, 0, bitmapData.Length);
            //string bitmapDataString = reader.ReadContentAsString();
            //reader.ReadEndElement();
            reader.ReadEndElement();

            //byte[] bitmapData = Convert.FromBase64String(bitmapDataString);
            ImageConverter converter = new ImageConverter();
            Data = (Bitmap)converter.ConvertFrom(bitmapData);
        }

        public void WriteXml(XmlWriter writer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ImageConverter converter = new ImageConverter();
                byte[] bitmapData = (byte[])converter.ConvertTo(Data, typeof(byte[]));

                writer.WriteStartElement("ByteCount");
                writer.WriteString(bitmapData.Length.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Image");
                //writer.WriteString(Convert.ToBase64String(bitmapData, 0, bitmapData.Length));
                writer.WriteBase64(bitmapData, 0, bitmapData.Length);
                writer.WriteEndElement();
            }
        }
    }
}
