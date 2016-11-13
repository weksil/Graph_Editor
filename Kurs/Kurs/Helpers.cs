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
        private List<Entry> entries = new List<Entry>();
        public XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement) throw new NullReferenceException();
            XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));
            List<Entry> list = serializer.Deserialize(reader) as List<Entry>;
            foreach (var item in entries)
            {
                this.Add((TKey)item.Key, (TValue)item.Value);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            entries.Clear();
            foreach (var key in Keys)
            {
                entries.Add(Entry.Create(key, this[key]));
            }
            var sr = new XmlSerializer(typeof(List<Entry>));
            sr.Serialize(writer, entries);
        }

        public static SerializableDictionary<TKey, TValue> Create(Entry[] entries)
        {
            var path = new SerializableDictionary<TKey, TValue>();
            foreach (var item in entries)
            {
                path.Add((TKey)item.Key, (TValue)item.Value);
            }
            return path;
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
