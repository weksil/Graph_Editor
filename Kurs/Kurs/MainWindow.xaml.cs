using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Kurs
{
    public partial class MainWindow : Window
    {
        #region Variables
        int currMod;
        bool saved = true;
        Border curNodeBorder;
        Point rightMOuseBotton;
        Graph graph = new Graph();
        Queue<Node> nodes = new Queue<Node>();
        Dictionary<int, Action<object>> MainOperations;
        #region Rename
        TextBlock tBlock;
        TextBox tBox;
        #endregion
        Key firstPressed = Key.None;
        Dictionary<Key, int> Mods = new Dictionary<Key, int>()
            {
            {Key.None,0 },
            {cnsAddNode,1 },
            {cnsAddEdge,2 },
            {cnsRemoveNode,3 },
            {cnsRenameNode,4 }
            };
        // Constants
        const Key cnsAddNode = Key.C;
        const Key cnsAddEdge = Key.E;
        const Key cnsRemoveNode = Key.D;
        const Key cnsRenameNode = Key.R;
        const Key cnsUndoKey = Key.Z;
        const Key cnsRedoKey = Key.Y;
        const Key cnsLoadKey = Key.L;
        const Key cnsSaveKey = Key.S;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            DataContext = graph;
            currMod = Mods[Key.None];
            MainOperations = new Dictionary<int, Action<object>>() {
                { Mods[cnsAddEdge] , CreateDelEdge} ,
                { Mods[cnsAddNode], CreateNode} ,
                { Mods[cnsRemoveNode] , DelNode} ,
                { Mods[cnsRenameNode] , RenameNode}
            };
            Tests tmp = new Tests();
            tmp.StartLogicTest();
            tmp.StartTimeTests();
        }
        #region Events
        private void Border_MouseRightButtonDown(Object sender, MouseButtonEventArgs e)
        {
            curNodeBorder = sender as Border;
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            curNodeBorder = sender as Border;
            if (currMod != Mods[Key.None] && currMod != Mods[cnsAddNode])
            {
                MainOperations[currMod].Invoke(sender);
            }
            if (currMod == Mods[Key.None])
                curNodeBorder.CaptureMouse();
            if (firstPressed == Key.None && currMod != Mods[cnsAddEdge] && currMod != Mods[cnsRenameNode])
                currMod = Mods[Key.None];
            e.Handled = true;
        }
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && currMod != Mods[cnsRenameNode])
            {
                MoveNode(sender, e.GetPosition(this));
            }
            e.Handled = true;
        }
        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            border.ReleaseMouseCapture();
            e.Handled = true;
        }
        private void ItemsControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currMod != Mods[Key.None])
                MainOperations[currMod].Invoke(e);
            if (currMod == Mods[cnsRenameNode])
                EndRename();
            if (firstPressed == Key.None)
            {
                currMod = Mods[Key.None];
                if (nodes.Count != 0)
                {
                    var tmp = nodes.Dequeue();
                    tmp.InvertSelect();
                }
            }
            e.Handled = true;
        }
        private void mainCanvas_MouseRightButtonDown(Object sender, MouseButtonEventArgs e)
        {
            rightMOuseBotton = e.GetPosition(this);
        }
        private void Window_KeyUp(Object sender, KeyEventArgs e)
        {

            if (e.Key != firstPressed && currMod != Mods[cnsRenameNode]) return;
            firstPressed = Key.None;
            if (e.Key == cnsAddEdge && currMod != Mods[cnsRenameNode])
            {
                if (nodes.Count != 0)
                {
                    var tmp = nodes.Dequeue();
                    tmp.InvertSelect();
                }
            }
            if (currMod != Mods[cnsRenameNode])
                currMod = Mods[Key.None];
            else if (nodes.Count == 0)
                currMod = Mods[Key.None];
            e.Handled = true;
        }
        private void Window_KeyDown(Object sender, KeyEventArgs e)
        {
            if (currMod != Mods[cnsRenameNode] && firstPressed == Key.None) firstPressed = e.Key;
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
            if (currMod == Mods[Key.None] && Keyboard.Modifiers == ModifierKeys.None)
                if (Mods.ContainsKey(firstPressed))
                    currMod = Mods[firstPressed];
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
            if (firstPressed == Key.None) currMod = Mods[Key.None];
            saved = false;
            var node = (sender as Border)?.DataContext as Node;
            if (node == null) return;
            graph.RemoveNode(node);
        }
        void RenameNode(object sender)
        {
            saved = false;
            Node node;
            if (sender is Node)
                node = sender as Node;
            else
                node = (sender as Border)?.DataContext as Node;
            if (node == null)
                return;
            if (nodes.Count > 0)
            {
                EndRename();
                return;
            }
            nodes.Enqueue(node);
            var tmp = (curNodeBorder.Child as Grid).Children;
            tBlock = tmp[0] as TextBlock;
            tBox = tmp[1] as TextBox;
            tBlock.Visibility = Visibility.Hidden;
            tBox.Visibility = Visibility.Visible;
            currMod = Mods[cnsRenameNode];
        }
        void EndRename()
        {
            saved = false;
            if (nodes.Count == 0)
            {
                currMod = Mods[Key.None];
                return;
            }
            var node = nodes.Dequeue();
            if (tBox.Text != "")
            {
                graph.RenameNode(node, tBox.Text);
            }
            tBlock.Visibility = Visibility.Visible;
            tBox.Visibility = Visibility.Hidden;
            currMod = Mods[Key.None];
        }
        void CreateNode(object sender)
        {
            saved = false;
            Point curpos;
            if (sender as MouseButtonEventArgs != null)
                curpos = (sender as MouseButtonEventArgs).GetPosition(Container);
            else
                curpos = (Point)sender;
            curpos.Y -= +11;
            graph.AddNode(curpos);
            curpos.X += 20;
            curpos.Y += 10;
            graph.Nodes[graph.Nodes.Count - 1].SetCentr(curpos);
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
            dlg.Filter = String.Format("Graph Files |*{0}", graph.FileExt);
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
            dlg.Filter = String.Format("Graph Files |*{0}", graph.FileExt);
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
        void SaveImage(string Extension)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            var path = Path.GetDirectoryName(Application.ResourceAssembly.Location) + graph.FilePath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            dlg.InitialDirectory = path;
            dlg.DefaultExt = Extension;
            dlg.Filter = String.Format("Image Files |*{0}| All files|*.*", Extension);
            dlg.AddExtension = true;
            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                int Height = (int)Container.ActualHeight;
                int Width = (int)Container.ActualWidth;
                RenderTargetBitmap bmp = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(Container);
                string file = dlg.FileName;
                BitmapEncoder encoder = null;
                if (Extension == ".gif")
                    encoder = new GifBitmapEncoder();
                else if (Extension == ".png")
                    encoder = new PngBitmapEncoder();
                else if (Extension == ".jpg")
                    encoder = new JpegBitmapEncoder();
                else if (Extension == ".bmp")
                    encoder = new BmpBitmapEncoder();
                else if (Extension == ".tif")
                    encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using (Stream stm = File.Create(file))
                {
                    encoder.Save(stm);
                }
            }
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
        private void MenuItem_Click_Save_png(Object sender, RoutedEventArgs e)
        {
            SaveImage(".png");
        }
        private void MenuItem_Click_Save_gif(Object sender, RoutedEventArgs e)
        {
            SaveImage(".gif");
        }
        private void MenuItem_Click_Save_jpg(Object sender, RoutedEventArgs e)
        {
            SaveImage(".jpg");
        }
        private void MenuItem_Click_Save_bmp(Object sender, RoutedEventArgs e)
        {
            SaveImage(".bmp");
        }
        private void MenuItem_Click_Save_tif(Object sender, RoutedEventArgs e)
        {
            SaveImage(".tif");
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
            currMod = Mods[cnsAddNode];
        }
        private void MenuItem_Click_Rename(Object sender, RoutedEventArgs e)
        {
            currMod = Mods[cnsRenameNode];
        }
        private void MenuItem_Click_Delete(Object sender, RoutedEventArgs e)
        {
            currMod = Mods[cnsRemoveNode];
        }
        private void MenuItem_Click_Connect(Object sender, RoutedEventArgs e)
        {
            currMod = Mods[cnsAddEdge];
        }
        private void MenuItem_Click_Node_Fill_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.FillColor);
            if (dlg.ShowDialog() == true)
            {
                graph.ChangeNodeBaseFillColor(dlg.fillColor);
            }
        }
        private void MenuItem_Click_Node_Border_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.BorderColor);
            if (dlg.ShowDialog() == true)
            {
                graph.ChangeNodeBaseBorderColor(dlg.fillColor);
            }
        }
        private void MenuItem_Click_Edge_Color(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            dlg.SetColor(graph.EdgeColor);
            if (dlg.ShowDialog() == true)
            {
                graph.ChangeEngeColor(dlg.fillColor);
            }
        }
        private void Node_Click_Fill(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            var node = (sender as MenuItem).DataContext as Node;
            dlg.SetColor(node.GetFillColor);
            if (dlg.ShowDialog() == true)
            {
                graph.ChangeNodeFillColor(node, dlg.fillColor);
            }
        }
        private void Node_Click_Border(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var dlg = new ColorDialog();
            var node = (sender as MenuItem).DataContext as Node;
            dlg.SetColor(node.GetBorderColor);
            if (dlg.ShowDialog() == true)
            {
                graph.ChangeNodeFillColor(node, dlg.fillColor);
            }
        }
        private void Node_Click_Rename(Object sender, RoutedEventArgs e)
        {
            saved = false;
            currMod = Mods[cnsRenameNode];
            var node = (sender as MenuItem).DataContext as Node;
            RenameNode((sender as MenuItem).DataContext as Node);
        }
        private void Node_Click_Connect(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var node = (sender as MenuItem).DataContext as Node;
            currMod = Mods[cnsAddEdge];
            CreateDelEdge(node);
        }
        private void Node_Click_Delete(Object sender, RoutedEventArgs e)
        {
            saved = false;
            var node = (sender as MenuItem).DataContext as Node;
            graph.RemoveNode(node);
        }
        private void Canvas_Click_Add_Node(Object sender, RoutedEventArgs e)
        {
            CreateNode(rightMOuseBotton);
        }
        #endregion
    }
}