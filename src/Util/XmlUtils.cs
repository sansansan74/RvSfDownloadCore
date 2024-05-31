using System.Xml.Linq;

namespace RvSfDownloadCore.Util
{
    public static class XmlUtils
    {
        public static XElement CreateXmlObjectFromBytes(byte[] byteXml)
        {
            using (MemoryStream ms = new MemoryStream(byteXml))
            {
                return XElement.Load(ms);
            }
        }

        public static byte[] SaveXmlObjectToBytes(XElement root, int xmlLength)
        {
            using (var sm = new MemoryStream(xmlLength))
            {
                root.Save(sm);
                sm.Flush();
                return sm.ToArray();
            }
        }
    }

}
