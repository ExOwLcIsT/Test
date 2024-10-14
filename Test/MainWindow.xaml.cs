using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Test
{
    public partial class MainWindow : Window
    {
        private Dictionary<int, Point> vertices = new Dictionary<int, Point>();
        private Dictionary<int, Dictionary<int, double>> graphEdges = new Dictionary<int, Dictionary<int, double>>();
        private Dictionary<int, Ellipse> circles = new Dictionary<int, Ellipse>();
        private List<Path> edges = new List<Path>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawGraphButton_Click(object sender, RoutedEventArgs e)
        {
            DrawGraph();
        }
        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();
            vertices.Clear();
            graphEdges.Clear();
            circles.Clear();
            edges.Clear();

            string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string[] parts = line.Split(' ');
                int vertex1 = int.Parse(parts[0].Split('-')[0]);
                int vertex2 = int.Parse(parts[0].Split('-')[1]);
                double weight = double.Parse(parts[1]);

                if (!vertices.ContainsKey(vertex1))
                {
                    AddVertex(vertex1);
                }

                if (!vertices.ContainsKey(vertex2))
                {
                    AddVertex(vertex2);
                }

                AddEdge(vertex1, vertex2, weight);
            }
        }
        private void AddVertex(int vertexNumber)
        {
            double circleX = (GraphCanvas.ActualWidth / 4) + 50 + (vertices.Count % 5) * 100; // Вирівнювання по горизонталі
            double circleY = (GraphCanvas.ActualHeight / 2) + (vertices.Count / 5) * 100; // Вирівнювання по вертикалі

            Ellipse circle = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, circleX - circle.Width / 2);
            Canvas.SetTop(circle, circleY - circle.Height / 2);
            GraphCanvas.Children.Add(circle);

            vertices[vertexNumber] = new Point(circleX, circleY);
            circles[vertexNumber] = circle;

            TextBlock text = new TextBlock
            {
                Text = vertexNumber.ToString(),
                FontSize = 14,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(text, circleX - 10);
            Canvas.SetTop(text, circleY - 10);
            GraphCanvas.Children.Add(text);
        }

        private void AddEdge(int vertex1, int vertex2, double weight)
        {
            if (!graphEdges.ContainsKey(vertex1))
            {
                graphEdges[vertex1] = new Dictionary<int, double>();
            }
            graphEdges[vertex1][vertex2] = weight;

            if (!graphEdges.ContainsKey(vertex2))
            {
                graphEdges[vertex2] = new Dictionary<int, double>();
            }
            graphEdges[vertex2][vertex1] = weight;

            DrawEdge(vertex1, vertex2);
        }

        private void DrawEdge(int vertex1, int vertex2, Brush color = null)
        {
            Point p1 = vertices[vertex1];
            Point p2 = vertices[vertex2];

            // Розрахунок середини між вершинами
            double midX = (p1.X + p2.X) / 2;
            double midY = (p1.Y + p2.Y) / 2;

            // Використання еліпсів для дуг
            var arc = new Path();
            var pathFigure = new PathFigure { StartPoint = p1 };
            var arcSegment = new ArcSegment
            {
                Point = p2,
                Size = new Size(20, 20), // Розмір дуги
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            };

            pathFigure.Segments.Add(arcSegment);
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            arc.Data = pathGeometry;
            arc.StrokeThickness = 2;
            arc.Stroke = color ?? Brushes.Black;

            GraphCanvas.Children.Add(arc);
            edges.Add(arc);
        }

        private void FindPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(EndVertexTextBox.Text, out int endVertex))
            {
                // Calculate the shortest path
                var result = CalculateShortestPath(endVertex);

                // Display the path and length in the grid
                PathLengthTextBlock.Text = $"Shortest Path: {string.Join(" -> ", result.path)}";
                TotalLengthTextBlock.Text = $"Total Length: {result.length}";

                // Highlight the shortest path using the previous vertices
                HighlightShortestPath(endVertex, result.previousVertices);
            }
            else
            {
                MessageBox.Show("Invalid vertex. Please enter a valid integer.");
            }
        }

        private void HighlightShortestPath(int endVertex, Dictionary<int, int?> previousVertices)
        {
            DrawGraph();
            foreach (var circle in circles)
            {
                circle.Value.Fill = Brushes.LightBlue;
            }

            int? current = endVertex;
            while (current != null)
            {
                Ellipse circle = circles[(int)current];
                circle.Fill = Brushes.Red;

                int? previous = previousVertices[(int)current];
                if (previous != null)
                {
                    DrawEdge((int)previous, (int)current, Brushes.Red);
                }

                current = previous;
            }
        }
        private (List<string> path, int length, Dictionary<int, int?> previousVertices) CalculateShortestPath(int n)
        {
            List<string> ar = new List<string>();
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            Dictionary<int, int> H_dic = new Dictionary<int, int>();
            Dictionary<int, int?> previousVertices = new Dictionary<int, int?>();

            // Initialize H_dic and previousVertices for all vertices
            for (int i = 1; i <= n; i++)
            {
                H_dic[i] = int.MaxValue; // Set initial distance to max for unvisited
                previousVertices[i] = null; // No previous vertex initially
            }

            // Distance from start vertex (1) to itself is 0
            H_dic[1] = 0; // Start vertex distance to itself is 0

            // Simulate the input data
            foreach (var edge in graphEdges)
            {
                foreach (var target in edge.Value)
                {
                    string key = $"{edge.Key}-{target.Key}";
                    dictionary[key] = (int)target.Value; // Assuming weight is an int
                }
            }

            List<int> start = new List<int> { 1 }; // Start from vertex 1
            List<int> end = new List<int>();

            for (int i = 2; i <= n; i++)
            {
                end.Add(i);
            }

            while (end.Count != 0)
            {
                List<int> x = new List<int>();
                List<string> our_key = new List<string>();

                foreach (string i in dictionary.Keys)
                {
                    string[] el = i.Split('-');
                    int el1 = int.Parse(el[0]);
                    int el2 = int.Parse(el[1]);

                    if (start.Contains(el1) && !start.Contains(el2))
                    {
                        our_key.Add(i);
                        int sum = H_dic[el1];
                        x.Add(dictionary[i] + sum);
                    }
                }

                if (x.Count == 0) break; // Prevent errors if no edges are found

                int min_val = int.MaxValue;
                int ind = -1;

                for (int j = 0; j < x.Count; j++)
                {
                    if (x[j] < min_val)
                    {
                        min_val = x[j];
                        ind = j;
                    }
                }

                string[] finalEl = our_key[ind].Split('-');
                int el2Final = int.Parse(finalEl[1]);
                start.Add(el2Final);
                end.Remove(el2Final);

                // Update previous vertices
                previousVertices[el2Final] = int.Parse(finalEl[0]);
                H_dic[el2Final] = min_val;
            }

            // Extract the path from previousVertices
            List<string> path = new List<string>();
            int? current = n; // Start from the end vertex
            while (current != null)
            {
                path.Add(current.ToString());
                current = previousVertices[(int)current];
            }
            path.Reverse(); // Reverse to get the correct order

            int totalLength = H_dic.ContainsKey(n) ? H_dic[n] : int.MaxValue; // The length to the last vertex

            return (path, totalLength, previousVertices);
        }



    }
}
