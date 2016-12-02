using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text;

namespace Kurs
{
    public partial class MainWindow : Window
    {
        #region Variables
        // Private Variables
        int CurrMod;
        bool saved = true;
        Key firstPressed = Key.None;
        Point rightMOuseBotton;
        object CurObject;
        Graph graph = new Graph();
        Queue<Node> nodes = new Queue<Node>();
        Dictionary<int, Action<object>> MainOperations;
        #region Rename
        TextBlock tBlock;
        TextBox tBox;
        #endregion

        Dictionary<Key, int> Mods = new Dictionary<Key, int>()
            {
            {Key.None,0 },
            {cnsAddNode,1 },
            {cnsAddEdge,2 },
            {cnsRemoveNode,3 },
            {cnsRenameNode,4 }
            };

        // Constants
        public const Key cnsAddNode = Key.C;
        public const Key cnsAddEdge = Key.E;
        public const Key cnsRemoveNode = Key.D;
        public const Key cnsRenameNode = Key.R;
        public const Key cnsUndoKey = Key.Z;
        public const Key cnsRedoKey = Key.Y;
        public const Key cnsLoadKey = Key.L;
        public const Key cnsSaveKey = Key.S;
        #endregion
        public MainWindow()
        {
            InitializeComponent();

            DataContext = graph;
            CurrMod = Mods[Key.None];
            MainOperations = new Dictionary<int, Action<object>>() {
                { Mods[cnsAddEdge] , CreateDelEdge} ,
                { Mods[cnsAddNode], CreateNode} ,
                { Mods[cnsRemoveNode] , DelNode} ,
                { Mods[cnsRenameNode] , RenameNode}
            };
        }
        #region Events
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (CurrMod != Mods[Key.None] && CurrMod != Mods[cnsAddNode])
            {
                MainOperations[CurrMod].Invoke(sender);
            }
            if (CurrMod == Mods[Key.None])
                border.CaptureMouse();
            if (firstPressed == Key.None && CurrMod != Mods[cnsAddEdge] && CurrMod != Mods[cnsRenameNode])
                CurrMod = Mods[Key.None];
            e.Handled = true;
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && CurrMod != Mods[cnsRenameNode])
            {
                MoveNode(sender, e.GetPosition(this));
            }
            e.Handled = true;
        }
        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            border.ReleaseMouseCapture();
            tbtest.Text = CurrMod.ToString(); //test
            e.Handled = true;
        }
        private void Border_MouseRightButtonDown(Object sender, MouseButtonEventArgs e)
        {
                CurObject = sender;
        }
        private void ItemsControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrMod != Mods[Key.None])
                MainOperations[CurrMod].Invoke(e);
            if (CurrMod == Mods[cnsRenameNode])
                EndRename();
            if (firstPressed == Key.None)
            {
                CurrMod = Mods[Key.None];
                if (nodes.Count != 0)
                {
                    var tmp = nodes.Dequeue();
                    tmp.InvertSelect();
                }
            }
            tbtest.Text = CurrMod.ToString(); //test
            e.Handled = true;
        }
        private void mainCanvas_MouseRightButtonDown(Object sender, MouseButtonEventArgs e)
        {
            rightMOuseBotton = e.GetPosition(this);
        }
        private void Window_KeyUp(Object sender, KeyEventArgs e)
        {

            if (e.Key != firstPressed && CurrMod != Mods[cnsRenameNode]) return;
            firstPressed = Key.None;
            if (e.Key == cnsAddEdge && CurrMod != Mods[cnsRenameNode])
            {
                if (nodes.Count != 0)
                {
                    var tmp = nodes.Dequeue();
                    tmp.InvertSelect();
                }
            }
            if (CurrMod != Mods[cnsRenameNode])
                CurrMod = Mods[Key.None];
            else if (nodes.Count == 0)
                CurrMod = Mods[Key.None];
            tbtest.Text = CurrMod.ToString(); //test
            e.Handled = true;
        }
        private void Window_KeyDown(Object sender, KeyEventArgs e)
        {
            if (CurrMod != Mods[cnsRenameNode] && firstPressed == Key.None) firstPressed = e.Key;
            if (e.Key == cnsUndoKey && Keyboard.Modifiers == ModifierKeys.Control)
            {
                saved = false;
                graph.Undo();
            }
            if (e.Key == cnsRedoKey && Keyboard.Modifiers == ModifierKeys.Control)
            {
                saved = false;
                graph.Redo();
            }
            if (e.Key == cnsSaveKey && Keyboard.Modifiers == ModifierKeys.Control)
            {
                QuickSave();
            }
            if (CurrMod == Mods[Key.None] && Keyboard.Modifiers == ModifierKeys.None)
                if (Mods.ContainsKey(firstPressed))
                    CurrMod = Mods[firstPressed];
            if (firstPressed == cnsSaveKey)
            {
                firstPressed = Key.None;
                Save();
            }
            if (firstPressed == cnsLoadKey)
            {
                firstPressed = Key.None;
                Load();
            }
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void TextBox_KeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EndRename();
            }
        }
        #endregion
        #region Operations
        void CreateDelEdge(object sender)
        {
            saved = false;
            Node node = sender as Node;
            if (node == null)
            {
                var border = (sender as Border);
                if (border == null) return;
                node = (sender as Border)?.DataContext as Node;
                if (node == null) return;
            }
            node.InvertSelect();
            if (node.Selected) nodes.Enqueue(node);
            else nodes.Dequeue();
            if (nodes.Count == 2)
            {
                var lastNode = nodes.Dequeue();
                lastNode.InvertSelect();
                graph.ConnectNodes(lastNode, nodes.Peek());
            }
        }
        void DelNode(object sender)
        {
            if (firstPressed == Key.None) CurrMod = Mods[Key.None];
            saved = false;
            var border = (sender as Border);
            if (border == null) return;
            var node = (sender as Border).DataContext as Node;
            if (node == null) return;
            graph.RemoveNode(node);
        }
        void RenameNode(object sender)
        {
            saved = false;
            Node node = (sender as Border)?.DataContext as Node;
            if (node == null)
                return;
            if (nodes.Count > 0)
            {
                EndRename();
                return;
            }
            nodes.Enqueue(node);
            var tmp = ((sender as Border).Child as Grid).Children;
            tBlock = tmp[0] as TextBlock;
            tBox = tmp[1] as TextBox;
            tBlock.Visibility = Visibility.Hidden;
            tBox.Visibility = Visibility.Visible;
            CurrMod = Mods[cnsRenameNode];
        }
        void EndRename()
        {
            saved = false;
            if (nodes.Count == 0)
            {
                CurrMod = Mods[Key.None];
                return;
            }
            var node = nodes.Dequeue();
            if (tBox.Text != "")
            {
                graph.RenameNode(node, tBox.Text);
            }
            tBlock.Visibility = Visibility.Visible;
            tBox.Visibility = Visibility.Hidden;
            CurrMod = Mods[Key.None];
            tbtest.Text = CurrMod.ToString(); //test
        }
        void CreateNode(object sender)
        {
            saved = false;
            Point curpos;
            if (sender as MouseButtonEventArgs != null)
                curpos = (sender as MouseButtonEventArgs).GetPosition(this);
            else
                curpos = (Point)sender;
            curpos.Y -= (int)mainGrid.RowDefinitions[0].ActualHeight + 11;
            graph.AddNode(new Point(0, 0));
            graph.Nodes[graph.Nodes.Count - 1].Move(curpos);
        }
        void MoveNode(object sender, Point curPos)
        {
            saved = false;
            var border = sender as Border;
            var node = border.DataContext as Node;
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                border.CaptureMouse();
                curPos.Y -= (int)mainGrid.RowDefinitions[0].ActualHeight;
                node.SetCentr(curPos);
                curPos.Y -= (int)border.ActualHeight / 2;
                curPos.X -= (int)border.ActualWidth / 2;
                node.Move(curPos);
            }
        }
        void Load()
        {
            Save();
            OpenFileDialog dlg = new OpenFileDialog();
            var path = Path.GetDirectoryName(Application.ResourceAssembly.Location) + graph.FilePath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            dlg.InitialDirectory = path;
            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                graph = graph.Load(dlg.OpenFile());
                DataContext = graph;
                saved = true;
            }

        }
        void Save()
        {
            if (saved) return;
            SaveFileDialog dlg = new SaveFileDialog();
            var path = Path.GetDirectoryName(Application.ResourceAssembly.Location) + graph.FilePath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            dlg.InitialDirectory = path;
            dlg.DefaultExt = graph.FileExt;
            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                graph.Save(dlg.FileName);
                saved = true;
            }
        }
        void QuickSave()
        {
            if (saved) return;
            graph.Save();
            saved = true;
        }
        #endregion
        #region Menu
        private void MenuItem_Click_Load(Object sender, RoutedEventArgs e)
        {
            Load();
        }
        private void MenuItem_Click_Save(Object sender, RoutedEventArgs e)
        {
            Save();
        }
        private void MenuItem_Click_QuickSave(Object sender, RoutedEventArgs e)
        {
            QuickSave();
        }
        private void MenuItem_Click_New(Object sender, RoutedEventArgs e)
        {
            Save();
            graph = new Graph();
            DataContext = graph;
        }
        private void MenuItem_Click_Undo(Object sender, RoutedEventArgs e)
        {
            saved = false;
            graph.Undo();
        }
        private void MenuItem_Click_Redo(Object sender, RoutedEventArgs e)
        {
            saved = false;
            graph.Redo();
        }
        private void MenuItem_Click_Add(Object sender, RoutedEventArgs e)
        {
            saved = false;
            CurrMod = Mods[cnsAddNode];
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void MenuItem_Click_Rename(Object sender, RoutedEventArgs e)
        {
            CurrMod = Mods[cnsRenameNode];
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void MenuItem_Click_Delete(Object sender, RoutedEventArgs e)
        {
            CurrMod = Mods[cnsRemoveNode];
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void MenuItem_Click_Connect(Object sender, RoutedEventArgs e)
        {
            CurrMod = Mods[cnsAddEdge];
            tbtest.Text = CurrMod.ToString(); //test
        }
        private void MenuItem_Click_Node_Fill_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.FillColor);
            if (dlg.ShowDialog() == true)
            {
                graph.FillColor = dlg.fillColor;
            }
        }
        private void MenuItem_Click_Node_Border_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.BorderColor);
            if (dlg.ShowDialog() == true)
            {
                graph.BorderColor = dlg.fillColor;
            }
        }
        private void MenuItem_Click_Edge_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.EdgeColor);
            if (dlg.ShowDialog() == true)
            {
                graph.EdgeColor = dlg.fillColor;
            }
        }
        private void Node_Click_Fill(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            var node = (sender as MenuItem).DataContext as Node;
            dlg.SetColor(node.GetFillColor());
            if (dlg.ShowDialog() == true)
            {
                node.SetFillColor(dlg.fillColor);
            }
        }
        private void Node_Click_Border(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            var node = (sender as MenuItem).DataContext as Node;
            dlg.SetColor(node.GetBorderColor());
            if (dlg.ShowDialog() == true)
            {
                node.SetBorderColor(dlg.fillColor);
            }
        }
        private void Node_Click_Rename(Object sender, RoutedEventArgs e)
        {
            saved = false;
            CurrMod = Mods[cnsRenameNode];
            RenameNode(CurObject);
        }
        private void Node_Click_Connect(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var node = (sender as MenuItem).DataContext as Node;
            CurrMod = Mods[cnsAddEdge];
            CreateDelEdge(node);
        }
        private void Node_Click_Delete(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var node = (sender as MenuItem).DataContext as Node;
            graph.DeleteNode(node);
        }
        private void Canvas_Click_Add_Node(Object sender, RoutedEventArgs e)
        {
            CreateNode(rightMOuseBotton);
        }
        #endregion
    }
}