using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Xml;

namespace Kurs
{
    class Tests
    {
        StringBuilder Results = new StringBuilder();
        List<Action> logicTests = new List<Action>();
        List<Action> timeTests = new List<Action>();
        Graph controlTimegraph = new Graph();
        public Tests()
        {
            logicTests.Add(testConnectNode);
            logicTests.Add(testDelNode);
            logicTests.Add(testUnconnectNode);
            timeTests.Add(() => controlTimegraph.RemoveNode(controlTimegraph.Nodes[0]));
            timeTests.Add(() => controlTimegraph.ChangeEngeColor(Colors.Gold));
            timeTests.Add(() => controlTimegraph.Save());
            timeTests.Add(LoadTest);
        }
        #region Logic
        public void StartLogicTest()
        {
            Results.Clear();
            foreach (var item in logicTests)
            {
                item.Invoke();
            }
            StreamWriter sw = File.CreateText("LogicResult.txt");
            sw.WriteLine(Results.ToString());
            sw.Close();
        }
        void testConnectNode()
        {
            Graph controlgraph = new Graph();
            controlgraph.AddNode(new Point(0, 0));
            controlgraph.AddNode(new Point(0, 0));
            controlgraph.ConnectNodes(controlgraph.Nodes[0], controlgraph.Nodes[1]);
            if (controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) && controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID))
            {
                Results.AppendLine(nameof(testConnectNode) + " : ok");
            }
            else
            {
                Results.AppendLine(nameof(testConnectNode) + " : error");
            }
            controlgraph.Undo();
            if (!(controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) || controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID)))
            {
                Results.AppendLine(nameof(testConnectNode) + " undo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testConnectNode) + " undo : error");
            }
            controlgraph.Redo();
            if (controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) && controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID))
            {
                Results.AppendLine(nameof(testConnectNode) + " redo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testConnectNode) + " redo : error");
            }
        }
        void testDelNode()
        {
            Graph controlgraph = new Graph();
            controlgraph.AddNode(new Point(0, 0));
            controlgraph.AddNode(new Point(0, 0));
            var node0 = controlgraph.Nodes[0];
            var node1 = controlgraph.Nodes[1];
            controlgraph.RemoveNode(controlgraph.Nodes[0]);
            if (controlgraph.Nodes[0] == node1 && controlgraph.Nodes.Count == 1)
            {
                Results.AppendLine(nameof(testDelNode) + " : ok");
            }
            else
            {
                Results.AppendLine(nameof(testDelNode) + " : error");
            }
            controlgraph.Undo();
            if (controlgraph.Nodes[1] == node0 && controlgraph.Nodes.Count == 2)
            {
                Results.AppendLine(nameof(testDelNode) + " undo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testDelNode) + " undo : error");
            }
            controlgraph.Redo();
            if (controlgraph.Nodes[0] == node1 && controlgraph.Nodes.Count == 1)
            {
                Results.AppendLine(nameof(testDelNode) + " redo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testDelNode) + " redo : error");
            }
        }
        void testUnconnectNode()
        {
            Graph controlgraph = new Graph();
            controlgraph.AddNode(new Point(0, 0));
            controlgraph.AddNode(new Point(0, 0));
            controlgraph.ConnectNodes(controlgraph.Nodes[0], controlgraph.Nodes[1]);
            controlgraph.UnconnectNodes(controlgraph.Nodes[0], controlgraph.Nodes[1]);
            if (!(controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) || controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID)))
            {
                Results.AppendLine(nameof(testUnconnectNode) + " : ok");
            }
            else
            {
                Results.AppendLine(nameof(testUnconnectNode) + " : error");
            }
            controlgraph.Undo();
            if (controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) && controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID))
            {
                Results.AppendLine(nameof(testUnconnectNode) + " undo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testUnconnectNode) + " undo : error");
            }
            controlgraph.Redo();
            if (!(controlgraph.Nodes[0].Path.ContainsKey(controlgraph.Nodes[1].ID) || controlgraph.Nodes[1].Path.ContainsKey(controlgraph.Nodes[0].ID)))
            {
                Results.AppendLine(nameof(testUnconnectNode) + " redo : ok");
            }
            else
            {
                Results.AppendLine(nameof(testUnconnectNode) + " redo : error");
            }
        }
        #endregion
        #region Time
        public void StartTimeTests()
        {
            Results.Clear();
            foreach (var item in timeTests)
            {
                TimeOper(item);
            }
            StreamWriter sw = File.CreateText("TimeResult.txt");
            sw.WriteLine(Results.ToString());
            sw.Close();
        }
        public void LoadTest()
        {
            Stream sw = File.Open(Path.GetDirectoryName(Application.ResourceAssembly.Location) + controlTimegraph.FilePath + controlTimegraph.FileName,FileMode.Open);
            var tmp = controlTimegraph.Load(sw);
            sw.Close();
        }
        void TimeOper(Action oper)
        {
            controlTimegraph.Nodes.Clear();
            controlTimegraph.Edges.Clear();
            int number = 10;
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.AddNode(new Point(0, 0));
            }
            for (int i = 0; i < number; i++)
            {
                for (int j = i + 1; j < number; j++)
                {
                    controlTimegraph.ConnectNodes(controlTimegraph.Nodes[i], controlTimegraph.Nodes[j]);
                }
            }
            Stopwatch sWatch = new Stopwatch();
            sWatch.Start();
            oper.Invoke();
            sWatch.Stop();
            Results.AppendLine("Nodes : 10 \t|Edges : 45 \t| Time : " + sWatch.ElapsedTicks);
            sWatch.Reset();

            controlTimegraph.Nodes.Clear();
            controlTimegraph.Edges.Clear();
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.AddNode(new Point(0, 0));
            }
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.ConnectNodes(controlTimegraph.Nodes[i % number], controlTimegraph.Nodes[(i + 1) % number]);
            }
            sWatch.Start();
            oper.Invoke();
            sWatch.Stop();
            Results.AppendLine("Nodes : 10 \t|Edges : 10 \t| Time : " + sWatch.ElapsedTicks);
            sWatch.Reset();

            number = 100;
            controlTimegraph.Nodes.Clear();
            controlTimegraph.Edges.Clear();
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.AddNode(new Point(0, 0));
            }
            for (int i = 0; i < number; i++)
            {
                for (int j = i + 1; j < number; j++)
                {
                    controlTimegraph.ConnectNodes(controlTimegraph.Nodes[i], controlTimegraph.Nodes[j]);
                }
            }
            sWatch.Start();
            oper.Invoke();
            sWatch.Stop();
            Results.AppendLine("Nodes : 100 \t|Edges : 4950 \t| Time : " + sWatch.ElapsedTicks);
            sWatch.Reset();

            controlTimegraph.Nodes.Clear();
            controlTimegraph.Edges.Clear();
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.AddNode(new Point(0, 0));
            }
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.ConnectNodes(controlTimegraph.Nodes[i % number], controlTimegraph.Nodes[(i + 1) % number]);
            }
            sWatch.Start();
            oper.Invoke();
            sWatch.Stop();
            Results.AppendLine("Nodes : 100 \t|Edges : 100 \t| Time : " + sWatch.ElapsedTicks);
            sWatch.Reset();

            number = 1000;
            controlTimegraph.Nodes.Clear();
            controlTimegraph.Edges.Clear();
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.AddNode(new Point(0, 0));
            }
            for (int i = 0; i < number; i++)
            {
                controlTimegraph.ConnectNodes(controlTimegraph.Nodes[i % number], controlTimegraph.Nodes[(i + 1) % number]);
            }
            sWatch.Start();
            oper.Invoke();
            sWatch.Stop();
            Results.AppendLine("Nodes : 1000 \t|Edges : 1000 \t| Time : " + sWatch.ElapsedTicks);
            sWatch.Reset();
        }
        #endregion
    }
}
