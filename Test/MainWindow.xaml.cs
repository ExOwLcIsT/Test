﻿using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
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
        private Dictionary<int, TextBlock> numbers = new Dictionary<int, TextBlock>();
        private List<System.Windows.Shapes.Path> edges = new List<System.Windows.Shapes.Path>();
        private HashSet<int> verts = new HashSet<int>();
        Dictionary<int, int> levels = new Dictionary<int, int>();

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
            verts.Clear();
            verts.Add(1);
            try
            {
                // Парсинг введених даних для графу
                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
                adjacencyList[1] = new List<int>();
                try
                {
                    foreach (var line in lines)
                    {
                        string[] parts = line.Split(' ');
                        int vertex1 = int.Parse(parts[0].Split('-')[0]);
                        int vertex2 = int.Parse(parts[0].Split('-')[1]);
                        double weight = double.Parse(parts[1]);
                        verts.Add(vertex1);
                        verts.Add(vertex2);

                        if (!adjacencyList.ContainsKey(vertex1))
                            adjacencyList[vertex1] = new List<int>();
                        adjacencyList[vertex1].Add(vertex2);
                    }
                }
                catch { throw new Exception("Wrong input!\nExample for input:\n1-2 5\n2-3 4\nInput must contain only integer values and '-' sign"); }
                // Алгоритм BFS для визначення рівнів
                Dictionary<int, int> vertexLevels = GetVertexLevels(adjacencyList);

                // Додаємо вершини, малюємо граф
                foreach (var vertex in verts)
                {
                    if (!vertices.ContainsKey(vertex))
                    {
                        AddVertex(vertex, vertexLevels[vertex]);
                    }
                }

                foreach (var line in lines)
                {
                    string[] parts = line.Split(' ');
                    int vertex1 = int.Parse(parts[0].Split('-')[0]);
                    int vertex2 = int.Parse(parts[0].Split('-')[1]);
                    double weight = double.Parse(parts[1]);

                    AddEdge(vertex1, vertex2, weight);
                }

                foreach (var v in verts)
                {
                    DrawVertex(v);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Метод для визначення рівнів вершин за допомогою BFS
        private Dictionary<int, int> GetVertexLevels(Dictionary<int, List<int>> adjacencyList)
        {
            Queue<int> queue = new Queue<int>();
            HashSet<int> visited = new HashSet<int>();

            // Початково додаємо першу вершину на рівень 0
            int startVertex = verts.First(); // перший елемент списку вершин
            queue.Enqueue(startVertex);
            levels[startVertex] = 0;
            visited.Add(startVertex);

            // Основний обхід BFS
            while (queue.Count > 0)
            {
                int vertex = queue.Dequeue();
                int currentLevel = levels[vertex];

                if (adjacencyList.ContainsKey(vertex))
                {
                    foreach (var neighbor in adjacencyList[vertex])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            levels[neighbor] = currentLevel + 1; // Наступний рівень
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // Визначаємо рівні для вершин, які не мають вихідних дуг
            int maxLevel = levels.Values.Count > 0 ? levels.Values.Max() : 0;
            foreach (var vertex in verts)
            {
                if (!levels.ContainsKey(vertex))
                {
                    levels[vertex] = maxLevel + 1; // Встановлюємо найнижчий рівень
                }
            }

            return levels;
        }

        // Оновлений метод AddVertex для розміщення вершин по рівнях
        private void AddVertex(int vertexNumber, int level)
        {
            double spacingY = 100; // Відстань між рівнями по вертикалі
            double spacingX = 75; // Відстань між вершинами на одному рівні

            // Отримуємо всі вершини, які мають однаковий рівень
            var verticesAtLevel = verts.Where(v => levels[v] == level).ToList();
            int indexAtLevel = verticesAtLevel.IndexOf(vertexNumber); // Знаходимо індекс цієї вершини на своєму рівні

            // Визначаємо позицію
            double posX = indexAtLevel * spacingX + spacingX; // Позиція по горизонталі, без відступу зліва
            double posY = level * spacingY + 50; // Відстань рівня від верху Canvas

            Ellipse circle = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, posX - circle.Width / 2);
            Canvas.SetTop(circle, posY - circle.Height / 2);
            vertices[vertexNumber] = new Point(posX, posY);
            circles[vertexNumber] = circle;

            TextBlock text = new TextBlock
            {
                Text = vertexNumber.ToString(),
                FontSize = 14,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(text, posX - 10);
            Canvas.SetTop(text, posY - 10);
            numbers[vertexNumber] = text;
        }

        private void DrawVertex(int vertexNumber)
        {
            GraphCanvas.Children.Remove(circles[vertexNumber]);
            GraphCanvas.Children.Add(circles[vertexNumber]);
            GraphCanvas.Children.Remove(numbers[vertexNumber]);
            GraphCanvas.Children.Add(numbers[vertexNumber]);
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

            // Calculate control points for Bezier curve
            double controlX = (p1.X + p2.X) / 2;
            double controlY = Math.Min(p1.Y, p2.Y) - 50; // Adjust control point to create a curve

            var bezier = new System.Windows.Shapes.Path();
            var pathFigure = new PathFigure { StartPoint = p1 };
            var bezierSegment = new BezierSegment
            {
                Point1 = new Point(controlX, p1.Y),
                Point2 = new Point(controlX, p2.Y),
                Point3 = p2
            };

            pathFigure.Segments.Add(bezierSegment);
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            bezier.Data = pathGeometry;
            bezier.StrokeThickness = 2;
            bezier.Stroke = color ?? Brushes.Black;

            GraphCanvas.Children.Add(bezier);
            edges.Add(bezier);

            // Calculate a point on the curve closer to the start (e.g., 10% of the way from start)
            Point arrowPosition = GetBezierPoint(bezierSegment, 0.6); // Point closer to the start (10% of the way)

            // Calculate direction vector for the arrow
            Vector direction = arrowPosition - p1; // Direction from start to arrow position
            direction.Normalize();

            // Draw arrow
            DrawArrow(arrowPosition, direction, color ?? Brushes.Black);
        }

        // Метод для визначення точки на кривій Безьє на основі параметра t
        private Point GetBezierPoint(BezierSegment bezierSegment, double t)
        {
            double x = Math.Pow(1 - t, 2) * bezierSegment.Point1.X
                       + 2 * (1 - t) * t * bezierSegment.Point2.X
                       + Math.Pow(t, 2) * bezierSegment.Point3.X;

            double y = Math.Pow(1 - t, 2) * bezierSegment.Point1.Y
                       + 2 * (1 - t) * t * bezierSegment.Point2.Y
                       + Math.Pow(t, 2) * bezierSegment.Point3.Y;

            return new Point(x, y);
        }

        private void DrawArrow(Point position, Vector direction, Brush color)
        {
            // Довжина стрілки
            double arrowLength = 6;
            double arrowWidth = 6;

            // Розрахунок точок трикутника для стрілки
            Point arrowTip = position;
            Point basePoint1 = new Point(
                arrowTip.X - arrowLength * direction.X + arrowWidth * direction.Y,
                arrowTip.Y - arrowLength * direction.Y - arrowWidth * direction.X
            );
            Point basePoint2 = new Point(
                arrowTip.X - arrowLength * direction.X - arrowWidth * direction.Y,
                arrowTip.Y - arrowLength * direction.Y + arrowWidth * direction.X
            );

            // Створюємо трикутник для стрілки
            var arrowHead = new Polygon
            {
                Points = new PointCollection { arrowTip, basePoint1, basePoint2 },
                Fill = color
            };

            // Додаємо стрілку на Canvas
            GraphCanvas.Children.Add(arrowHead);
        }
        private void FindPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var endVertex = int.Parse(EndVertexTextBox.Text);
                if (!verts.Contains(endVertex))
                {
                    throw new Exception();
                }
                var result = CalculateShortestPath(endVertex);

                PathLengthTextBlock.Text = $"Shortest way: {string.Join(" -> ", result.path)}";
                TotalLengthTextBlock.Text = $"Shortest way length: {result.length}";

                HighlightShortestPath(endVertex, result.previousVertices);
            }
            catch
            {
                MessageBox.Show("Wrong input of vertice");
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

                foreach (var v in verts)
                {
                    DrawVertex(v);
                }
                current = previous;
            }
        }
        private (List<string> path, int length, Dictionary<int, int?> previousVertices) CalculateShortestPath(int n)
        {
            string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, int> edges = new();
            foreach (var line in lines)
            {
                string[] parts = line.Split(' ');
                string key = parts[0];
                int weight = int.Parse(parts[1]);
                if (edges.ContainsKey(key))
                {
                    edges[key] = Math.Min(weight, edges[key]);
                }
                else
                    edges[key] = weight;
            }
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

                foreach (string i in edges.Keys)
                {
                    string[] el = i.Split('-');
                    int el1 = int.Parse(el[0]);
                    int el2 = int.Parse(el[1]);

                    if (start.Contains(el1) && !start.Contains(el2))
                    {
                        our_key.Add(i);
                        int sum = H_dic[el1];
                        x.Add(edges[i] + sum);
                    }
                }

                if (x.Count == 0) break;

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

        private void ShowMinCutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> ar = new List<string>();
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                Dictionary<List<string>, int> ways = new();
                Dictionary<string, int> r = new();

                // Зберігаємо всі можливі шляхи до n
                List<List<string>> allPaths;
                List<int> pathWeights;
                int sum = 0;
                // Функція для пошуку всіх шляхів
                void DFS(string currentVertex, string targetVertex, List<string> path, HashSet<string> visited, int currentMinWeight)
                {
                    // Додаємо поточну вершину до шляху
                    path.Add(currentVertex);
                    visited.Add(currentVertex);

                    // Якщо досягли кінцевої вершини, зберігаємо шлях і мінімальну вагу
                    if (currentVertex == targetVertex)
                    {
                        allPaths.Add(new List<string>(path));
                        pathWeights.Add(currentMinWeight);  // Зберігаємо мінімальну вагу ребра на шляху
                    }
                    else
                    {
                        // Ітеруємо всі ребра, які починаються з поточної вершини
                        foreach (var edge in dictionary)
                        {
                            var parts = edge.Key.Split("-");
                            string from = parts[0];
                            string to = parts[1];

                            // Якщо поточна вершина є початковою в ребрі та кінцева ще не відвідана
                            if (from == currentVertex && !visited.Contains(to))
                            {
                                int edgeWeight = edge.Value;
                                // Оновлюємо мінімальну вагу
                                int newMinWeight = Math.Min(currentMinWeight, edgeWeight);

                                // Викликаємо DFS для наступної вершини
                                DFS(to, targetVertex, path, visited, newMinWeight);
                            }
                        }
                    }

                    // Вихід з вершини - видаляємо її зі шляху та списку відвіданих
                    path.RemoveAt(path.Count - 1);
                    visited.Remove(currentVertex);
                }

                void ProcessPaths(int targetVertex)
                {
                    if (targetVertex == 1)
                    {

                        return;
                    }
                    // Виконуємо цикл доти, доки є можливість знайти шлях до n
                    while (true)
                    {
                        // Очищаємо списки для зберігання шляхів та мінімальних ваг
                        allPaths = new List<List<string>>();
                        pathWeights = new List<int>();

                        // Пошук всіх шляхів від вершини "1" до вершини "n"
                        List<string> path = new List<string>();
                        HashSet<string> visited = new HashSet<string>();

                        // Початкове значення мінімальної ваги - максимальне можливе значення
                        int initialMinWeight = int.MaxValue;

                        // Викликаємо DFS для початкової вершини "1" та цільової вершини n
                        DFS("1", targetVertex.ToString(), path, visited, initialMinWeight);

                        // Якщо не знайдено жодного шляху, виходимо з циклу
                        if (allPaths.Count == 0)
                        {
                            break;
                        }

                        // Знаходимо індекс шляху з найбільшою мінімальною вагою
                        int maxWeight = pathWeights.Max();
                        int indexMaxWeightPath = pathWeights.IndexOf(maxWeight);

                        // Отримуємо шлях із найбільшою мінімальною вагою
                        List<string> maxWeightPath = allPaths[indexMaxWeightPath];
                        sum += maxWeight;
                        // Виводимо шлях із найбільшою мінімальною вагою та її значення
                        ways.Add(maxWeightPath, maxWeight);

                        // Оновлюємо ваги ребер у цьому шляху, віднімаючи від них найбільшу вагу
                        for (int i = 0; i < maxWeightPath.Count - 1; i++)
                        {
                            string from = maxWeightPath[i];
                            string to = maxWeightPath[i + 1];
                            string edgeKey = from + "-" + to;

                            if (dictionary.ContainsKey(edgeKey))
                            {
                                // Віднімаємо найбільшу вагу від ребра
                                dictionary[edgeKey] -= maxWeight;

                                // Якщо вага стає 0 або меншою, видаляємо ребро
                                if (dictionary[edgeKey] <= 0)
                                {
                                    r.Add(edgeKey, dictionary[edgeKey]);
                                    dictionary.Remove(edgeKey);
                                }
                            }
                        }
                    }
                }

                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    ar.Add(line);
                }

                // Зберігаємо ребра у форматі "початок-кінець вага"
                foreach (var i in ar)
                {
                    var parts = i.Split(" ");
                    string key = parts[0];
                    int value = int.Parse(parts[1]);
                    dictionary[key] = value;
                }

                // Зчитуємо кінцеву вершину (наприклад, кінцева вершина - 8)
                if (int.TryParse(EndVertexTextBox.Text, out int n))
                {
                    if (!verts.Contains(n))
                    {
                        throw new Exception("Wrong input, must contain vertice from graph");
                    }
                }
                else
                {
                    throw new Exception("Wrong input, must contain vertice from graph");
                }
                // Запускаємо процес пошуку шляхів і зміни ваг
                ProcessPaths(n);
                if (r.Count > 0)
                {
                    MinimumCrossSection.Text = ("Min-cuts:\n");

                    DrawGraph();
                    foreach (var i in r)
                    {
                        int previous = int.Parse(i.Key.Split("-")[0]);
                        int current = int.Parse(i.Key.Split("-")[1]);
                        DrawEdge(previous, current, Brushes.Magenta);
                        MinimumCrossSection.Text += (string.Join(", ", i.Key) + " : " + i.Value + "\n");
                    }
                }
                else
                {
                    MinimumCrossSection.Text = ("No min-cuts:\n");
                }
                if (n == 1)
                    MinimumCrossSection.Text = "Maximum flow: infinity";
                else
                    MinimumCrossSection.Text += ($"Maximum flow: {sum}");

                foreach (var v in verts)
                {
                    DrawVertex(v);
                }

            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
        }
        private HashSet<int> GetReachableVertices(int source)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                visited.Add(current);

                foreach (var neighbor in graphEdges[current])
                {
                    if (!visited.Contains(neighbor.Key) && neighbor.Value > 0) // Залишкова пропускна здатність
                    {
                        queue.Enqueue(neighbor.Key);
                    }
                }
            }

            return visited;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", // Фільтр для файлів
                Title = "Select a text file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string fileContent = File.ReadAllText(openFileDialog.FileName);

                InputTextBox.Text = fileContent;
            }
        }
    }
}
