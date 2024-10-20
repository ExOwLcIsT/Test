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
            double spacingY = 100; // Відстань між рівнями по вертикалі
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
            try
            {
                List<string> ar = new List<string>();
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                Dictionary<string, int> inverse = new Dictionary<string, int>();
                Dictionary<int, int> H_dic = new Dictionary<int, int> { { 1, 0 } };

                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
                foreach (string line in lines)
                {
                    ar.Add(line);
                }

                foreach (string i in ar)
                {
                    string[] split = i.Split(' ');
                    dictionary[split[0]] = int.Parse(split[1]);
                    inverse[split[0]] = 0;
                }

                Console.WriteLine("Dictionary:");
                foreach (var kvp in dictionary)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                Console.WriteLine("Inverse:");
                foreach (var kvp in inverse)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                Console.WriteLine("Input End:");
                int n = int.Parse(EndVertexTextBox.Text);

                List<string> result = new List<string>();
                List<string> our_key = new List<string>();

                int start = 1;
                List<string> t = new List<string>();
                List<int> max_val = new List<int>();
                List<string> r = new List<string>();
                bool status = false;
                List<List<string>> ways = new List<List<string>>();
                List<List<string>> bad = new List<List<string>>();
                Dictionary<int, int> H_Bad_dic = new Dictionary<int, int>();
                List<int> min_r = new List<int>();

                bool pos_w = false;
                int last_el = 0;
                List<int> g = new List<int>();

                // Define other required variables
                List<string> possible_ways = new List<string>();
                List<string> completed = new List<string>();
                List<string> remember = new List<string>();
                List<int> st = new List<int>();

                foreach (var item in dictionary)
                {
                    string[] elements = item.Key.Split('-');
                    int el1 = int.Parse(elements[0]);
                    int el2 = int.Parse(elements[1]);
                    if (el2 == n)
                    {
                        possible_ways.Add(item.Key);
                        if (el1 != 1)
                        {
                            g.Add(el1);
                        }
                    }

                    if (el2 > last_el)
                    {
                        last_el = el2;
                    }
                }

                string skip_way = string.Empty;

                while (!status)
                {
                    if (string.IsNullOrEmpty(skip_way))
                    {
                        t.Clear();
                    }

                    max_val.Clear();
                    List<string> repeated = new List<string>();
                    bool second_cycle = false;

                    while (start != n)
                    {
                        List<int> x = new List<int>();
                        our_key.Clear();

                        foreach (var item in dictionary)
                        {
                            string[] elements = item.Key.Split('-');
                            int el1 = int.Parse(elements[0]);
                            int el2 = int.Parse(elements[1]);

                            if (pos_w)
                                break;

                            if (start == el1)
                            {
                                if (repeated.Contains(item.Key))
                                {
                                    skip_way = t[t.IndexOf(item.Key) + 1];
                                    t.RemoveRange(t.IndexOf(item.Key) + 1, t.Count - (t.IndexOf(item.Key) + 1));
                                    start = el2;
                                    second_cycle = true;
                                    break;
                                }

                                if (n != last_el && (g.Contains(el2) || (possible_ways.Contains(item.Key) && !remember.Contains(item.Key))))
                                {
                                    if (possible_ways.Contains(item.Key))
                                    {
                                        completed.Add(item.Key);
                                    }
                                    our_key.Add(item.Key);
                                    int sub = dictionary[item.Key] - inverse[item.Key];
                                    x.Add(sub);
                                    pos_w = true;
                                    break;
                                }

                                if (!st.Contains(el2) && item.Key != skip_way)
                                {
                                    repeated.Add(item.Key);
                                    our_key.Add(item.Key);
                                    int sub = dictionary[item.Key] - inverse[item.Key];

                                    if (sub == 0)
                                    {
                                        bad.Add(t);
                                        if (!r.Contains(item.Key))
                                        {
                                            r.Add(item.Key);
                                        }
                                    }

                                    x.Add(sub);
                                }
                            }
                        }

                        pos_w = false;

                        if (x.Count == 0)
                        {
                            break;
                        }

                        max_val.Add(x.Max());
                        if (x.Max() == 0) break;

                        int ind = x.IndexOf(x.Max());
                        t.Add(our_key[ind]);

                        string[] selected = our_key[ind].Split('-');
                        start = int.Parse(selected[1]);
                    }

                    if (!second_cycle)
                    {
                        skip_way = string.Empty;
                        int min_val = max_val.Min();
                        ways.Add(new List<string>(t));
                        min_r.Add(min_val);

                        foreach (var i in t)
                        {
                            inverse[i] += min_val;
                        }

                        foreach (var item in dictionary)
                        {
                            int sub = dictionary[item.Key] - inverse[item.Key];
                            if (sub == 0)
                            {
                                bad.Add(t);
                                if (!r.Contains(item.Key))
                                {
                                    r.Add(item.Key);
                                }
                            }
                        }

                        if (completed.SequenceEqual(possible_ways) || possible_ways.Intersect(r).Count() == possible_ways.Count)
                        {
                            status = true;
                            break;
                        }

                        start = 1;

                        for (int n1 = 1; n1 <= last_el; n1++)
                        {
                            int k1 = dictionary.Keys.Count(key => key.StartsWith(n1.ToString() + "-"));
                            H_dic[n1] = k1;
                        }

                        for (int i = 1; i <= last_el; i++)
                        {
                            H_Bad_dic[i] = 0;
                        }

                        foreach (var i in dictionary.Keys)
                        {
                            if (r.Contains(i))
                            {
                                int el1 = int.Parse(i.Split('-')[0]);
                                H_Bad_dic[el1]++;
                            }
                        }

                        // Process completion and continue condition
                        int numb = 0;
                        List<int> check = new List<int>();

                        foreach (var i in r)
                        {
                            int el1 = int.Parse(i.Split('-')[0]);
                            int el2 = int.Parse(i.Split('-')[1]);
                            if (g.Contains(el1))
                            {
                                g.Remove(el1);
                            }
                            if (g.Contains(el2))
                            {
                                g.Remove(el2);
                            }

                            foreach (var j in dictionary)
                            {
                                int el_1 = int.Parse(j.Key.Split('-')[0]);
                                int el_2 = int.Parse(j.Key.Split('-')[1]);
                                if (el1 == el_2 && !check.Contains(el_1))
                                {
                                    numb++;
                                    check.Add(el_1);
                                }
                            }
                        }

                        bool need_to_stop = check.Contains(1);
                        check.Remove(1);
                        if (!check.Any()) need_to_stop = true;

                        while (!need_to_stop)
                        {
                            foreach (var i in check.ToList())
                            {
                                foreach (var j in dictionary)
                                {
                                    int el_1 = int.Parse(j.Key.Split('-')[0]);
                                    int el_2 = int.Parse(j.Key.Split('-')[1]);
                                    if (i == el_2 && !check.Contains(el_1))
                                    {
                                        numb++;
                                        check.Add(el_1);
                                    }
                                }
                                check.Remove(i);
                            }

                            if (check.Contains(1))
                            {
                                need_to_stop = true;
                            }
                        }

                        bool one_cycle = false;
                        st.Clear();
                        List<int> use = new List<int>();

                        if (numb == 0)
                        {
                            numb = 2;
                        }

                        for (int k = 0; k < numb; k++)
                        {
                            if (status) break;

                            if (one_cycle)
                            {
                                foreach (var i in dictionary)
                                {
                                    int el1 = int.Parse(i.Key.Split('-')[0]);
                                    int el2 = int.Parse(i.Key.Split('-')[1]);

                                    if (H_Bad_dic[el2] == H_dic[el2] && H_Bad_dic[el2] != 0 && !st.Contains(el2) && !r.Contains(i.Key))
                                    {
                                        H_Bad_dic[el1]++;
                                        st.Add(el2);
                                        use.Add(el1);
                                    }
                                }
                            }

                            foreach (var i in dictionary)
                            {
                                int el1 = int.Parse(i.Key.Split('-')[0]);
                                int el2 = int.Parse(i.Key.Split('-')[1]);

                                if (H_Bad_dic[el2] == H_dic[el2] && H_Bad_dic[el2] != 0 && !use.Contains(el1) && !r.Contains(i.Key))
                                {
                                    H_Bad_dic[el1]++;
                                    if (!st.Contains(el2)) st.Add(el2);
                                    if (!use.Contains(el1)) use.Add(el1);
                                }

                                if (H_Bad_dic[1] == H_dic[1] && H_Bad_dic[1] != 0)
                                {
                                    status = true;
                                    break;
                                }
                                if (H_Bad_dic[n] == H_dic[n] && H_Bad_dic[n] != 0)
                                {
                                    status = true;
                                    break;
                                }
                            }

                            one_cycle = true;
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
                MinimumCrossSection.Text = "Максимальний потік: " + min_r.Sum();
            }
            catch { MessageBox.Show("Сталася помилка"); }
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
