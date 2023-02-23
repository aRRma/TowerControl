using System.Xml;
using System.Xml.Serialization;

namespace TowerControl.Helpers
{
    public class XmlSerializerHelper<T>
    {
        private XmlSerializer s = null;
        private Type type = null;

        public XmlSerializerHelper()
        {
            this.type = typeof(T);
            this.s = new XmlSerializer(this.type);
        }

        public T Deserialize(string xml)
        {
            TextReader reader = new StringReader(xml);
            return Deserialize(reader);
        }

        public T Deserialize(XmlDocument doc)
        {
            TextReader reader = new StringReader(doc.OuterXml);
            return Deserialize(reader);
        }

        public T Deserialize(TextReader reader)
        {
            T o = (T)s.Deserialize(reader);
            reader.Close();
            return o;
        }

        public XmlDocument Serialize(T rootclass)
        {
            string xml = StringSerialize(rootclass);
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            return doc;
        }

        public string StringSerialize(T rootclass)
        {
            TextWriter w = WriterSerialize(rootclass);
            string xml = w.ToString();
            w.Close();
            return xml.Trim();
        }

        private TextWriter WriterSerialize(T rootclass)
        {
            TextWriter w = new StringWriter();
            this.s = new XmlSerializer(this.type);
            s.Serialize(w, rootclass);
            w.Flush();
            return w;
        }
    }
}