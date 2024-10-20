using Microsoft.Win32;
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
            try
            {
                // Парсинг введених даних для графу
                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
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
                // Обробка винятків
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

            return levels;
        }

        // Оновлений метод AddVertex для розміщення вершин по рівнях
        private void AddVertex(int vertexNumber, int level)
        {
            double spacingY = 120; // Відстань між рівнями по вертикалі
            double spacingX = 100; // Відстань між вершинами на одному рівні

            // Отримуємо всі вершини, які мають однаковий рівень
            var verticesAtLevel = verts.Where(v => levels[v] == level).ToList();
            int indexAtLevel = verticesAtLevel.IndexOf(vertexNumber); // Знаходимо індекс цієї вершини на своєму рівні

            // Визначаємо позицію
            double posX = indexAtLevel * spacingX; // Позиція по горизонталі, без відступу зліва
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
        }

        private void FindPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(EndVertexTextBox.Text, out int endVertex))
            {
                var result = CalculateShortestPath(endVertex);

                PathLengthTextBlock.Text = $"Shortest Path: {string.Join(" -> ", result.path)}";
                TotalLengthTextBlock.Text = $"Total Length: {result.length}";

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

                foreach (var v in verts)
                {
                    DrawVertex(v);
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

        private void ShowMinCutButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> ar = new List<string>();
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            Dictionary<string, int> inverse = new Dictionary<string, int>();

            try
            {
                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    ar.Add(line);
                }

                foreach (string i in ar)
                {
                    string[] keyValue = i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0];
                        int value;
                        if (int.TryParse(keyValue[1], out value))
                        {
                            dictionary[key] = value;
                            inverse[key] = 0;
                        }
                        else
                        {
                            Console.WriteLine($"Помилка перетворення значення {keyValue[1]} в int.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Неправильний формат рядка: {i}");
                    }
                }
                int n = int.Parse(EndVertexTextBox.Text);
                List<string> t = new List<string>();
                List<int> max_val = new List<int>();
                List<string> r = new List<string>();
                bool status = false;
                List<List<string>> ways = new List<List<string>>();
                List<string> bad = new List<string>();

                Dictionary<int, int> H_dic = new Dictionary<int, int>();
                Dictionary<int, int> H_Bad_dic = new Dictionary<int, int>();

                List<int> min_r = new List<int>();
                int start = 1;

                while (!status)
                {
                    t.Clear();
                    max_val.Clear();

                    while (start != n)
                    {
                        List<int> x = new List<int>();
                        List<string> our_key = new List<string>();

                        foreach (var kvp in dictionary)
                        {
                            string[] keys = kvp.Key.Split('-');
                            if (keys.Length == 2)
                            {
                                int el1, el2;
                                if (int.TryParse(keys[0], out el1) && int.TryParse(keys[1], out el2))
                                {
                                    if (start == el1)
                                    {
                                        if (!r.Contains(kvp.Key))
                                        {
                                            our_key.Add(kvp.Key);
                                            int sub = dictionary[kvp.Key] - inverse[kvp.Key];
                                            if (sub == 0)
                                            {
                                                bad.Add(kvp.Key);
                                                if (!r.Contains(kvp.Key))
                                                    r.Add(kvp.Key);
                                            }
                                            x.Add(sub);
                                        }
                                    }
                                }
                            }
                        }

                        if (x.Count == 0)
                            break;

                        max_val.Add(x.Max());
                        if (x.Max() == 0)
                            break;

                        int ind = x.IndexOf(x.Max());
                        t.Add(our_key[ind]);
                        start = int.Parse(our_key[ind].Split('-')[1]);
                    }

                    if (t.Count == 0)
                        break;

                    ways.Add(new List<string>(t));
                    int min_val = max_val.Min();
                    min_r.Add(min_val);

                    foreach (string i in t)
                    {
                        inverse[i] += min_val;
                    }

                    foreach (var kvp in dictionary)
                    {
                        int sub = kvp.Value - inverse[kvp.Key];
                        if (sub == 0)
                        {
                            bad.Add(kvp.Key);
                            if (!r.Contains(kvp.Key))
                                r.Add(kvp.Key);
                        }
                    }

                    start = 1;

                    for (int n1 = 1; n1 <= n; n1++)
                    {
                        int k1 = 0;
                        foreach (var kvp in dictionary)
                        {
                            if (int.Parse(kvp.Key.Split('-')[0]) == n1)
                                k1++;
                        }
                        H_dic[n1] = k1;
                    }

                    H_Bad_dic.Clear();
                    for (int i = 1; i <= n; i++)
                        H_Bad_dic[i] = 0;

                    foreach (var kvp in dictionary)
                    {
                        if (r.Contains(kvp.Key))
                        {
                            int el1 = int.Parse(kvp.Key.Split('-')[0]);
                            if (H_Bad_dic.Keys.Contains(el1))
                                H_Bad_dic[el1]++;
                        }
                    }
                }
                DrawGraph();
                foreach (var way in ways)
                    Console.WriteLine(string.Join(", ", way));
                foreach (var item in r)
                {
                    int previous = int.Parse(item.Split("-")[0]);
                    int current = int.Parse(item.Split("-")[1]);
                    DrawEdge(previous, current, Brushes.Magenta);
                }
                foreach (var v in verts)
                {
                    DrawVertex(v);
                }
                _ = min_r;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
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
