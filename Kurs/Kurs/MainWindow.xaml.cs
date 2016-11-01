using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kurs
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        // Private Variables
        int CurrMod;
        Graph graph = new Graph();
        Point pressPoint;
        Queue<Node> nodes = new Queue<Node>();
        Dictionary<int , Action<object>> MainOperations;
        #region Rename txtBlocks
        TextBlock tBlock;
        TextBox tBox;
        #endregion

        Dictionary<Key , int> Mods = new Dictionary<Key , int>()
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
        #endregion
        public MainWindow()
        {
            InitializeComponent();

            DataContext = graph;

            MainOperations = new Dictionary<int , Action<object>>() {
                { Mods[cnsAddEdge] , CreateDelEdge} ,
                { Mods[cnsAddNode], CreateNode} ,
                { Mods[cnsRemoveNode] , DelNode} ,
                { Mods[cnsRenameNode] , RenameNode}
            };
        }

        #region Events
        private void Border_MouseDown(object sender , MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var border = sender as Border;
                if (CurrMod != Mods[ Key.None ] && CurrMod != Mods[ cnsAddNode ])
                {
                    MainOperations[ CurrMod ].Invoke(sender);
                }

                pressPoint = e.GetPosition(this);
                if (CurrMod == Mods[ Key.None ])
                    border.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Border_MouseMove(object sender , MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && CurrMod != Mods[ cnsRenameNode ])
            {
                MoveNode(sender , e.GetPosition(this));
            }
            e.Handled = true;
        }

        private void Border_MouseUp(object sender , MouseButtonEventArgs e)
        {
            var border = sender as Border;
            border.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void ItemsControl_MouseDown(object sender , MouseButtonEventArgs e)
        {
            if (CurrMod != Mods[ Key.None ])
                MainOperations[ CurrMod ].Invoke(e);
            if (CurrMod == Mods[ cnsRenameNode ])
                EndRename();
            e.Handled = true;
        }

        private void Window_KeyUp(Object sender , KeyEventArgs e)
        {
            if (e.Key == cnsAddEdge && CurrMod != Mods[ cnsRenameNode ])
            {
                if (nodes.Count != 0)
                {
                    var tmp = nodes.Dequeue();
                    tmp.InvertSelect();
                }
            }
            if (CurrMod != Mods[ cnsRenameNode ])
                CurrMod = Mods[ Key.None ];
            else if (nodes.Count == 0)
                CurrMod = Mods[ Key.None ];
            tbtest.Text = CurrMod.ToString(); //test
            e.Handled = true;
        }

        private void Window_KeyDown(Object sender , KeyEventArgs e)
        {
            if (e.Key == cnsUndoKey && Keyboard.Modifiers == ModifierKeys.Control)
            {
                graph.Undo();
            }
            if (e.Key == cnsRedoKey && Keyboard.Modifiers == ModifierKeys.Control)
            {
                graph.Redo();
            }
            if (CurrMod == Mods[ Key.None ] && Keyboard.Modifiers == ModifierKeys.None)
                if (Mods.ContainsKey(e.Key))
                    CurrMod = Mods[ e.Key ];
            tbtest.Text = CurrMod.ToString(); //test
        }

        private void TextBox_KeyDown(Object sender , KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EndRename();
            }
        }
        #endregion

        #region Operations
        #region LeftMouseDown
        void CreateDelEdge(object sender)
        {
            var node = ( sender as Border )?.DataContext as Node;
            if (node == null)
                return;
            node.InvertSelect();
            if (node.Selected)
            {
                nodes.Enqueue(node);
            }
            else
            {
                nodes.Dequeue();
            }
            if (nodes.Count == 2)
            {
                var lastNode = nodes.Dequeue();
                lastNode.InvertSelect();
                graph.ConnectNodes(lastNode , nodes.Peek());
            }
        }

        void DelNode(object sender)
        {
            var node = ( sender as Border )?.DataContext as Node;
            if (node == null)
                return;
            graph.RemoveNode(node);
        }

        void RenameNode(object sender)
        {
            var node = ( sender as Border )?.DataContext as Node;
            if (node == null)
                return;
            if (nodes.Count > 0)
            {
                EndRename();
                return;
            }
            nodes.Enqueue(node);
            var tmp = ( ( sender as Border ).Child as Grid ).Children;
            tBlock = tmp[ 0 ] as TextBlock;
            tBox = tmp[ 1 ] as TextBox;
            tBlock.Visibility = Visibility.Hidden;
            tBox.Visibility = Visibility.Visible;
            CurrMod = Mods[ cnsRenameNode ];
        }

        void EndRename()
        {
            if (nodes.Count == 0)
                return;
            var node = nodes.Dequeue();
            if (tBox.Text != "")
            {
                graph.RenameNode(node , tBox.Text);
            }
            tBlock.Visibility = Visibility.Visible;
            tBox.Visibility = Visibility.Hidden;
            CurrMod = Mods[ Key.None ];
            tbtest.Text = CurrMod.ToString(); //test
        }

        void CreateNode(object sender)
        {
            var curpos = ( sender as MouseButtonEventArgs ).GetPosition(this);
            graph.AddNode(curpos);
            pressPoint = curpos;
        }

        void MoveNode(object sender , Point curPos)
        {
            var border = sender as Border;
            var node = border.DataContext as Node;
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                border.CaptureMouse();
                var dR = curPos - pressPoint;
                node.Pos = node.Pos + dR;
                pressPoint = curPos;
            }
        }

        #endregion
        #region RightMouseDown

        #endregion
        #endregion
    }
}