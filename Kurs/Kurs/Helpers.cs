using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Kurs
{
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement) throw new NullReferenceException();
            reader.ReadStartElement();
            XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));
            List<Entry> list = serializer.Deserialize(reader) as List<Entry>;
            foreach (var item in list)
            {
                Add((TKey)item.Key, (TValue)item.Value);
            }
            reader.ReadEndElement();
        }
        public void WriteXml(XmlWriter writer)
        {
            List<Entry> entries = new List<Entry>();
            foreach (var key in Keys)
            {
                entries.Add(Entry.Create(key, this[key]));
            }
            var sr = new XmlSerializer(typeof(List<Entry>));
            sr.Serialize(writer, entries);
        }
    }
    public class Entry
    {
        public object Key;
        public object Value;
        public static Entry Create(object key, object value)
        {
            var tmp = new Entry();
            tmp.Key = key;
            tmp.Value = value;
            return tmp;
        }
    }
}
