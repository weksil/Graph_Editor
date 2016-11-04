using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Kurs
{
    class Graph
    {
        public ObservableCollection<Node> Nodes { get; }
        public ObservableCollection<Edge> Edges { get; }
        private int NodesCount ;

        private List<BaseCommand> History;
        private int historyStep;

        public Graph()
        {
            Nodes = new ObservableCollection<Node>();
            Edges = new ObservableCollection<Edge>();
            History = new List<BaseCommand>();
            NodesCount = 0;
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
            if (node1.Path.ContainsKey(node2))
            {
                UnconnectNodes(node1,node2);
                return;
            }
            NewOperation();
            var com = ComConnectNode.SetCommand(this, node1, node2);
            com.Execute();
            History.Add(com);
        }
        public void UnconnectNodes(Node node1, Node node2)
        {
            NewOperation();
            var com = ComUnconnectNode.SetCommand(this, node1, node2);
            com.Execute();
            History.Add(com);
        }

        #region Logic
        public void CreateEdge(Node a, Node b)
        {
            var res = Edge.Create(a, b);
            Edges.Add(res);
            a.Path.Add(b, res);
            b.Path.Add(a, res);
        }
        public void CreateNode(Point pos)
        {
            Nodes.Add(Node.Create(pos, NodesCount));
            NodesCount++;
        }
        public void CreateNode(Node node)
        {
            Nodes.Add(node);
            node.Text += "_del";
            foreach (var item in node.Path)
            {
                item.Key.Path.Add(node, item.Value);
                Edges.Add(item.Value);
            }
        }
        public void CreateNode(Point pos, string text)
        {
            Nodes.Add(Node.Create(pos, text, NodesCount));
            NodesCount++;
        }
        /// <summary>
        /// Find edge is connected node1 and node2. If edge was not created return null
        /// </summary>
        /// <returns></returns>
        public Edge FindEdge(Node node1, Node node2)
        {
            if (node1.Path.ContainsKey(node2))
                return node1.Path[node2];
            return null;
        }
        public void DeleteNode(Node node)
        {
            Nodes.Remove(node);
            foreach (var item in node.Path)
            {
                item.Key.Path.Remove(node);
                Edges.Remove(item.Value);
            }
            NodesCount--;
        }
        public void DeleteEdge(Node node1, Node node2)
        {
            Edge edge = FindEdge(node1, node2);
            if (edge == null)
                return; 
            edge.A.Path.Remove(edge.B);
            edge.B.Path.Remove(edge.A);
            Edges.Remove(edge);
        }
        #endregion
    }
    class Node : INotifyPropertyChanged
    {
        public string Text { get; set; }
        public Dictionary<Node, Edge> Path;
        Point pos;
        public int ID { get; set; }
        public void Rename(string name)
        {
            Text = name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Text"));
        }
        public Point Pos
        {
            get { return pos; }
            set
            {
                if (pos != value)
                {
                    pos = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Pos"));
                }
            }
        }

        bool selected;
        public bool Selected { get { return selected; } }
        public double SelectedOpacity
        {
            get { return selected ? 1 : 0; }
        }

        public void Select()
        {
            selected = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedOpacity"));
        }

        public void UnSelect()
        {
            selected = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedOpacity"));
        }

        public void InvertSelect()
        {
            selected = !selected;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedOpacity"));
        }

        public static Node Create(Point position, string text, int id)
        {
            Node res = new Node();
            res.pos = position;
            res.Text = text;
            res.Path = new Dictionary<Node, Edge>();
            res.ID = id;
            return res;
        }

        public static Node Create(Point position, int numb)
        {
            return Create(position, String.Format("Node{0}", numb), numb);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    class Edge
    {
        public Node A { get; set; }
        public Node B { get; set; }
        public int Weight { get; set; }
        public static Edge Create(Node a, Node b)
        {
            return Edge.Create(a,b,1);
        }
        public static Edge Create(Node a , Node b, int weight)
        {
            Edge res = new Edge();
            res.A = a;
            res.B = b;
            res.Weight = weight;
            return res;
        }
    }
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
}
