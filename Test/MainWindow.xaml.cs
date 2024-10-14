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
            double circleX = 50 + (vertices.Count % 5) * 100; // Вирівнювання по горизонталі
            double circleY = (GraphCanvas.ActualHeight/2)  + (vertices.Count / 5) * 100; // Вирівнювання по вертикалі

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
                FindShortestPath(1, endVertex);
            }
            else
            {
                MessageBox.Show("Invalid vertex. Please enter a valid integer.");
            }
        }

        private void FindShortestPath(int startVertex, int endVertex)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();
            Dictionary<int, int?> previousVertices = new Dictionary<int, int?>();

            foreach (var vertex in vertices)
            {
                distances[vertex.Key] = double.MaxValue;
                previousVertices[vertex.Key] = null;
            }

            distances[startVertex] = 0;

            var priorityQueue = new SortedSet<Tuple<double, int>>(Comparer<Tuple<double, int>>.Create((x, y) =>
            {
                int result = x.Item1.CompareTo(y.Item1);
                return result == 0 ? x.Item2.CompareTo(y.Item2) : result;
            }));

            priorityQueue.Add(Tuple.Create(0.0, startVertex));

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Min;
                priorityQueue.Remove(current);

                double currentDistance = current.Item1;
                int currentVertex = current.Item2;

                if (currentVertex == endVertex)
                {
                    break;
                }

                foreach (var edge in graphEdges[currentVertex])
                {
                    int neighbor = edge.Key;
                    double weight = edge.Value;

                    double newDist = currentDistance + weight;
                    if (newDist < distances[neighbor])
                    {
                        priorityQueue.Remove(Tuple.Create(distances[neighbor], neighbor));
                        distances[neighbor] = newDist;
                        previousVertices[neighbor] = currentVertex;
                        priorityQueue.Add(Tuple.Create(newDist, neighbor));
                    }
                }
            }

            HighlightShortestPath(endVertex, previousVertices);
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
    }
}
