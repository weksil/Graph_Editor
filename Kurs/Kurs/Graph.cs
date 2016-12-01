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
using System.Windows.Media;

namespace Kurs
{
    [Serializable()]
    public class Graph : IXmlSerializable
    {
        public ObservableCollection<Edge> Edges { get; }
        public ObservableCollection<Node> Nodes { get; }
        public Color BorderColor { get; set; }
        public Color FillColor { get; set; }
        private Color edgeColor;
        private Dictionary<int, Node> nodesId = new Dictionary<int, Node>();
        private Dictionary<int, Edge> edgesId = new Dictionary<int, Edge>();
        private int NodesCount;
        private int EdgesCount;
        private string fileName = "Graph.xml";
        private string filePath = @"Saves\";
        public string FileName { get { return fileName; } }
        public string FilePath { get { return '\\' + filePath; } }
        public string FileExt { get { return ".xml"; } }

        public Color EdgeColor {
            get { return edgeColor; }
            set
            {
                edgeColor = value;
                foreach (var item in Edges)  item.ChangeColor();
            } }

        private List<BaseCommand> History;
        private int historyStep;

        public Graph()
        {
            Nodes = new ObservableCollection<Node>();
            Edges = new ObservableCollection<Edge>();
            History = new List<BaseCommand>();
            BorderColor = Colors.Blue;
            FillColor = Colors.LightBlue;
            EdgeColor = Colors.Black;
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
            Save(filePath, fileName);
        }
        public void Save(string path, string name)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
                return;
            if (!Directory.Exists(path))
                throw new NullReferenceException("missing directory");
            Stream sw = File.Create(path + name);
            XmlSerializer ser = new XmlSerializer(this.GetType());
            ser.Serialize(sw, this);
            sw.Close();
            fileName = name;
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
            return Load(filePath, fileName);
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
            tmp.SetFillColor(FillColor);
            tmp.SetBorderColor(BorderColor);
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

            var edgesCount = reader.ReadElementContentAsInt();
            var nodesCount = reader.ReadElementContentAsInt();

            EdgesCount = reader.ReadElementContentAsInt();
            NodesCount = reader.ReadElementContentAsInt();

            var serclr = new XmlSerializer(BorderColor.GetType());
            BorderColor = (Color)serclr.Deserialize(reader);
            FillColor = (Color)serclr.Deserialize(reader);
            EdgeColor = (Color)serclr.Deserialize(reader);

            reader.ReadStartElement();
            if (edgesCount != 0)
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    cur_Edge = ser_Edges.Deserialize(reader) as Edge;
                    Edges.Add(cur_Edge);
                    edgesId.Add(cur_Edge.ID, cur_Edge);
                    cur_Edge.SetParent(this);
                    reader.MoveToContent();
                }
                reader.ReadEndElement();
            }

            reader.ReadStartElement();
            if (nodesCount != 0)
            {
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

        }
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Edghes.count");
            writer.WriteValue(Edges.Count);
            writer.WriteEndElement();

            writer.WriteStartElement("Nodes.count");
            writer.WriteValue(Nodes.Count);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(EdgesCount));
            writer.WriteValue(EdgesCount);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(NodesCount));
            writer.WriteValue(NodesCount);
            writer.WriteEndElement();

            var serclr = new XmlSerializer(BorderColor.GetType());
            serclr.Serialize(writer, BorderColor);
            serclr.Serialize(writer, FillColor);
            serclr.Serialize(writer, EdgeColor);

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
        private Point pos;
        private Point centr;
        private int id;
        private Color borderColor;
        private Color fillColor;
        public Brush BorderColor { get { return new SolidColorBrush(borderColor); } }
        public Brush FillColor { get { return new SolidColorBrush(fillColor); } }
        public int ID { get { return id; } }
        public string Text { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public SerializableDictionary<int, int> Path;
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
        }
        public Point Centr
        {
            get { return centr; }
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
        public void Move(Point moveTo)
        {
            pos = moveTo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(pos)));
        }
        public void SetCentr(Point newCentr)
        {
            centr = newCentr;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(centr)));
        }
        public void SetFillColor(Color newclr)
        {
            fillColor = newclr;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(fillColor)));
        }
        public void SetBorderColor(Color newclr)
        {
            borderColor = newclr;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(borderColor)));
        }
        public static Node Create(Point position, string text, int id)
        {
            Node res = new Node();
            res.pos = position;
            res.Text = text;
            res.Path = new SerializableDictionary<int, int>();
            res.id = id;
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

            id = reader.ReadElementContentAsInt();

            Text = reader.ReadElementContentAsString();

            reader.ReadStartElement();
            var x = reader.ReadElementContentAsDouble();
            var y = reader.ReadElementContentAsDouble();
            pos = new Point(x, y);
            reader.ReadEndElement();

            reader.ReadStartElement();
            x = reader.ReadElementContentAsDouble();
            y = reader.ReadElementContentAsDouble();
            centr = new Point(x, y);
            reader.ReadEndElement();

            var serclr = new XmlSerializer(borderColor.GetType());
            borderColor =  (Color)serclr.Deserialize(reader);
            fillColor = (Color)serclr.Deserialize(reader);

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

            writer.WriteStartElement(nameof(Centr));

            writer.WriteStartElement(nameof(centr.X));
            writer.WriteValue(centr.X);
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(centr.Y));
            writer.WriteValue(centr.Y);
            writer.WriteEndElement();

            writer.WriteEndElement();

            var serclr = new XmlSerializer(borderColor.GetType());
            serclr.Serialize(writer, borderColor);
            serclr.Serialize(writer, fillColor);
        }
    }
    [Serializable()]
    public class Edge: INotifyPropertyChanged
    {

        public Node B { get { return parent.Find(b); } }
        [field: NonSerialized()]
        private int a;
        [field: NonSerialized()]
        private int b;
        public Brush FillColor { get { return new SolidColorBrush(parent.EdgeColor); } }
        private Graph parent;

        public event PropertyChangedEventHandler PropertyChanged;
        public void ChangeColor()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FillColor)));
        }
        public Node A { get { return parent.Find(a); } }
        public int ID { get; set; }
        public static Edge Create(Node a, Node b, int id)
        {
            Edge res = new Edge();
            res.Connect(a, b);
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
