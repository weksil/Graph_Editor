using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Kurs
{
    [Serializable()]
    public class Graph : IXmlSerializable
    {
        public ObservableCollection<Edge> Edges { get; }
        public ObservableCollection<Node> Nodes { get; }
        private Dictionary<int, Node> nodesId = new Dictionary<int, Node>();
        private Dictionary<int, Edge> edgesId = new Dictionary<int, Edge>();
        private int NodesCount;
        private int EdgesCount;

        private string fileName = "Graph.xml";
        private string filePath = @"Saves\";

        public string FileName { get { return fileName; } }
        public string FilePath { get { return '\\'+filePath; } }
        public string FileExt { get { return ".xml"; } }


        private List<BaseCommand> History;
        private int historyStep;
        public Graph()
        {
            Nodes = new ObservableCollection<Node>();
            Edges = new ObservableCollection<Edge>();
            History = new List<BaseCommand>();
        }
        public void Undo()
        {
            if (History.Count == 0 || (History.Count - historyStep - 1) < 0) return;
            History[History.Count - historyStep - 1].Undo();
            historyStep++;
        }
        public void Redo()
        {
            if (History.Count == 0 || historyStep == 0) return;
            History[History.Count - historyStep].Redo();
            historyStep--;
        }
        private void NewOperation()
        {
            if (historyStep == 0) return;
            History.RemoveRange(History.Count - historyStep, historyStep);
            historyStep = 0;
        }
        public void AddNode(Point pos, string text)
        {
            NewOperation();
            var add = ComCreateNode.SetCommand(this, pos, text);
            add.Execute();
            History.Add(add);
        }
        public void AddNode(Point pos)
        {
            AddNode(pos, "");
        }
        public void RemoveNode(Node node)
        {
            NewOperation();
            var com = ComDelNode.SetCommand(this, node);
            com.Execute();
            History.Add(com);
        }
        public void RenameNode(Node node, string text)
        {
            if (text.Length == 0) return;
            NewOperation();
            var com = ComRenameNode.SetCommand(node, text);
            com.Execute();
            History.Add(com);
        }
        public void ConnectNodes(Node node1, Node node2)
        {
            if (node1.Path.ContainsKey(node2.ID))
            {
                UnconnectNodes(node1, node2);
                return;
            }
            NewOperation();
            var com = ComConnectNode.SetCommand(this, node1, node2);
            com.Execute();
            History.Add(com);
        }
        public void ConnectNodes(int node1_ID, int node2_ID)
        {
            ConnectNodes(nodesId[node1_ID], nodesId[node2_ID]);
        }
        public void UnconnectNodes(Node node1, Node node2)
        {
            NewOperation();
            var com = ComUnconnectNode.SetCommand(this, node1, node2);
            com.Execute();
            History.Add(com);
        }
        public void Save()
        {
            Save(filePath , fileName);
        }
        public void Save(string path,string name)
        {
            if (!Directory.Exists(path))
                throw new NullReferenceException("missing directory");
            Stream sw = File.Create(path + name);
            XmlSerializer ser = new XmlSerializer(this.GetType());
            ser.Serialize(sw , this);
            sw.Close();
        }
        public Graph Load(Stream reader)
        {
            XmlSerializer ser = new XmlSerializer(this.GetType());
            var res = ser.Deserialize(reader) as Graph;
            reader.Close();
            return res;
        }
        public Graph Load(string path, string name)
        {

            if (Directory.Exists(path))
            {
                if (File.Exists(path + name))
                {
                    Stream sw = File.OpenRead(path + name);
                    return Load(sw);
                }
                else
                {
                    throw new NullReferenceException("missing file");
                }
            }
            return null;
        }
        public Graph Load()
        {
            return Load(filePath , fileName);
        }
        #region Logic
        public void CreateEdge(Node a, Node b)
        {
            var res = Edge.Create(a, b, EdgesCount);
            res.SetParent(this);
            Edges.Add(res);
            edgesId.Add(res.ID, res);
            a.Path.Add(b.ID, res.ID);
            b.Path.Add(a.ID, res.ID);
            EdgesCount++;
        }
        public void CreateNode(Point pos)
        {
            Node tmp = Node.Create(pos, NodesCount);
            Nodes.Add(tmp);
            nodesId.Add(tmp.ID, tmp);
            NodesCount++;
        }
        public void CreateNode(Node node)
        {
            Nodes.Add(node);
            nodesId.Add(node.ID, node);
            foreach (var item in node.Path)
            {

                nodesId[item.Key].Path.Add(node.ID, item.Value);
                Edges.Add(edgesId[item.Value]);
            }
            NodesCount++;
        }
        public void CreateNode(Point pos, string text)
        {
            Node tmp = Node.Create(pos, text, NodesCount);
            Nodes.Add(tmp);
            nodesId.Add(tmp.ID, tmp);
            NodesCount++;
        }
        public Edge FindEdge(Node node1, Node node2)
        {
            if (node1.Path.ContainsKey(node2.ID))
                return edgesId[node1.Path[node2.ID]];
            return null;
        }
        public void DeleteNode(Node node)
        {
            Nodes.Remove(node);
            foreach (var item in node.Path)
            {
                nodesId[item.Key].Path.Remove(node.ID);
                Edges.Remove(edgesId[item.Value]);
            }
            nodesId.Remove(node.ID);

        }
        public void DeleteEdge(Node node1, Node node2)
        {
            Edge edge = FindEdge(node1, node2);
            if (edge == null)
                return;
            edge.A.Path.Remove(edge.B.ID);
            edge.B.Path.Remove(edge.A.ID);
            Edges.Remove(edge);
            edgesId.Remove(edge.ID);
        }
        public Node Find(int id)
        {
            return nodesId[id];
        }
        #endregion
        #region Serialization
        public XmlSchema GetSchema()
        {
            return null;
        }
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement) return;
            reader.Read();
            var ser_Edges = new XmlSerializer(typeof(Edge));
            var ser_Nodes = new XmlSerializer(typeof(Node));
            Edge cur_Edge = null;
            Node cur_Node = null;

            EdgesCount = reader.ReadElementContentAsInt();
            NodesCount = reader.ReadElementContentAsInt();


            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                cur_Edge = ser_Edges.Deserialize(reader) as Edge;
                Edges.Add(cur_Edge);
                edgesId.Add(cur_Edge.ID, cur_Edge);
                cur_Edge.SetParent(this);
                reader.MoveToContent();
            }
            reader.ReadEndElement();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                cur_Node = ser_Nodes.Deserialize(reader) as Node;
                Nodes.Add(cur_Node);
                nodesId.Add(cur_Node.ID, cur_Node);
                foreach (var item in cur_Node.Path)
                {
                    edgesId[item.Value].Connect(cur_Node.ID, item.Key);
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();

        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(EdgesCount));
            writer.WriteValue(EdgesCount);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(NodesCount));
            writer.WriteValue(NodesCount);
            writer.WriteEndElement();

            var serEdges = new XmlSerializer(typeof(ObservableCollection<Edge>));
            serEdges.Serialize(writer, Edges);
            var serNodes = new XmlSerializer(typeof(ObservableCollection<Node>));
            serNodes.Serialize(writer, Nodes);
        }
        #endregion
    }
    [Serializable()]
    [XmlRoot(nameof(Node))]
    public class Node : INotifyPropertyChanged, IXmlSerializable
    {
        public int ID { get; set; }
        public string Text { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public SerializableDictionary<int, int> Path;
        private Point pos;
        [field: NonSerialized()]
        private bool selected;
        public void Rename(string name)
        {
            Text = name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
        public Point Pos
        {
            get { return pos; }
            set
            {
                if (pos != value)
                {
                    pos = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(pos)));
                }
            }
        }
        public bool Selected { get { return selected; } }
        public double SelectedOpacity
        {
            get { return selected ? 1 : 0; }
        }
        public void InvertSelect()
        {
            selected = !selected;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOpacity)));
        }

        public static Node Create(Point position, string text, int id)
        {
            Node res = new Node();
            res.pos = position;
            res.Text = text;
            res.Path = new SerializableDictionary<int, int>();
            res.ID = id;
            return res;
        }
        public static Node Create(Point position, int numb)
        {
            return Create(position, String.Format("Node{0}", numb), numb);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement) return;
            reader.Read();
            var pathSerializer = new XmlSerializer(typeof(Entry[]));
            reader.ReadStartElement();
            reader.ReadStartElement();
            Path = SerializableDictionary<int, int>.Create(pathSerializer.Deserialize(reader) as Entry[]);
            reader.ReadEndElement();
            reader.ReadEndElement();

            ID = reader.ReadElementContentAsInt();

            Text = reader.ReadElementContentAsString();

            reader.ReadStartElement();
            var x = reader.ReadElementContentAsDouble();
            var y = reader.ReadElementContentAsDouble();
            Pos = new Point(x, y);
            reader.ReadEndElement();
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {

            writer.WriteStartElement(nameof(Path));
            var ser = new XmlSerializer(typeof(SerializableDictionary<int, int>));
            ser.Serialize(writer, Path);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(ID));
            writer.WriteValue(ID);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(Text));
            writer.WriteValue(Text);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(Pos));

            writer.WriteStartElement(nameof(pos.X));
            writer.WriteValue(pos.X);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(pos.Y));
            writer.WriteValue(pos.Y);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
    [Serializable()]
    public class Edge
    {

        public Node B { get { return parent.Find(b); } }
        [field: NonSerialized()]
        private int a;
        [field: NonSerialized()]
        private int b;
        private Graph parent;
        public Node A { get { return parent.Find(a); } }
        public int ID { get; set; }
        public int Weight { get; set; }
        public static Edge Create(Node a, Node b, int id)
        {
            return Edge.Create(a, b, 1, id);
        }
        public static Edge Create(Node a, Node b, int weight, int id)
        {
            Edge res = new Edge();
            res.Connect(a, b);
            res.Weight = weight;
            res.ID = id;
            return res;
        }

        public void Connect(int A, int B)
        {
            if (a == A && b == B || a == B && b == A) return;
            a = A;
            b = B;
        }

        public void Connect(Node A, Node B)
        {
            Connect(A.ID, B.ID);
        }

        public void SetParent(Graph parent)
        {
            this.parent = parent;
        }
    }

    #region Commands
    interface BaseCommand
    {
        void Execute();
        void Undo();

        void Redo();
    }
    class ComCreateNode : BaseCommand
    {
        Graph _graph;
        Point _pos;
        string _text;
        Node _node;
        public static ComCreateNode SetCommand(Graph curgraph, Point pos, string text)
        {
            var tmp = new ComCreateNode();
            tmp._graph = curgraph;
            tmp._pos = pos;
            tmp._text = text;
            return tmp;
        }
        public void Execute()
        {
            if (_text.Length == 0)
            {
                _graph.CreateNode(_pos);
            }
            else
            {
                _graph.CreateNode(_pos, _text);
            }
            _node = _graph.Nodes.Last();
        }
        public void Undo()
        {
            if (_node == null) return;
            _graph.DeleteNode(_node);
        }
        public void Redo()
        {
            if (_node == null) return;
            _graph.CreateNode(_node);
        }
    }
    class ComDelNode : BaseCommand
    {
        Graph _graph;
        Node _node;
        public static ComDelNode SetCommand(Graph curgraph, Node node)
        {
            var tmp = new ComDelNode();
            tmp._graph = curgraph;
            tmp._node = node;
            return tmp;
        }
        public void Execute()
        {
            _graph.DeleteNode(_node);
        }
        public void Undo()
        {
            if (_node == null) return;
            _graph.CreateNode(_node);
        }
        public void Redo()
        {
            if (_node == null) return;
            _graph.DeleteNode(_node);
        }
    }
    class ComRenameNode : BaseCommand
    {
        Node _node;
        string _lastName;
        string _newName;
        public static ComRenameNode SetCommand(Node node, string text)
        {
            var tmp = new ComRenameNode();
            tmp._node = node;
            tmp._newName = text;
            return tmp;
        }
        public void Execute()
        {
            _lastName = _node.Text;
            _node.Rename(_newName);
        }
        public void Undo()
        {
            if (_node == null) return;
            _node.Rename(_lastName);
        }
        public void Redo()
        {
            if (_node == null) return;
            _node.Rename(_newName);
        }
    }
    class ComConnectNode : BaseCommand
    {
        Node _node1;
        Node _node2;
        Graph _graph;
        public static ComConnectNode SetCommand(Graph curgraph, Node node1, Node node2)
        {
            var tmp = new ComConnectNode();
            tmp._graph = curgraph;
            tmp._node1 = node1;
            tmp._node2 = node2;
            return tmp;
        }
        public void Execute()
        {
            _graph.CreateEdge(_node1, _node2);
        }
        public void Undo()
        {
            if (_node1 == null) return;
            _graph.DeleteEdge(_node1, _node2);
        }
        public void Redo()
        {
            if (_node1 == null) return;
            _graph.CreateEdge(_node1, _node2);
        }
    }
    class ComUnconnectNode : BaseCommand
    {
        Node _node1;
        Node _node2;
        Graph _graph;
        public static ComUnconnectNode SetCommand(Graph curgraph, Node node1, Node node2)
        {
            var tmp = new ComUnconnectNode();
            tmp._graph = curgraph;
            tmp._node1 = node1;
            tmp._node2 = node2;
            return tmp;
        }
        public void Execute()
        {
            _graph.DeleteEdge(_node1, _node2);
        }
        public void Undo()
        {
            if (_node1 == null) return;
            _graph.CreateEdge(_node1, _node2);
        }
        public void Redo()
        {
            if (_node1 == null) return;
            _graph.DeleteEdge(_node1, _node2);
        }
    }
    #endregion
}
