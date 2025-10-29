using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Core
{
    public class SerializableDictionary<K, V> where K : notnull
    {
        public SerializableDictionary()
        {

        }

        [XmlIgnore]
        private Dictionary<K, V> _dictionary = new Dictionary<K, V>();

        [XmlIgnore]
        public V this[K key]
        {
            get => _dictionary[key];

            set => _dictionary[key] = value;
        }

        public bool HasKey(K key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(K key, V value)
        {
            _dictionary[key] = value;
        }

        public IEnumerable<K> GetKeys()
        {
            return _dictionary.Keys;
        }

        //[XmlArray("items")]
        //[XmlArrayItem("item")]
        [XmlElement("item")]
        public SerializableKeyValuePair<K, V>[] SerializableProperties
        {
            get
            {
                return _dictionary.Select(kvp => new SerializableKeyValuePair<K, V>(kvp.Key, kvp.Value)).ToArray();
            }

            set
            {
                if (value is null)
                    _dictionary = new Dictionary<K, V>();
                else
                    _dictionary = value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }

    public class SerializableKeyValuePair<K, V>
    {
        private K _key;
        private V _value;

        public SerializableKeyValuePair()
        {
            _key = default;
            _value = default;
        }

        public SerializableKeyValuePair(K key, V value)
        {
            _key = key;
            _value = value;
        }

        [XmlAttribute("key")]
        public K Key { get => _key; set => _key = value; }

        [XmlElement("value")]
        public V Value { get => _value; set => _value = value; }
    }
}