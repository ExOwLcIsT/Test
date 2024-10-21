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
            if (int.TryParse(EndVertexTextBox.Text, out int endVertex))
            {
                var result = CalculateShortestPath(endVertex);

                PathLengthTextBlock.Text = $"Найкоротший шлях: {string.Join(" -> ", result.path)}";
                TotalLengthTextBlock.Text = $"Довжина найкоротшого шляху: {result.length}";

                HighlightShortestPath(endVertex, result.previousVertices);
            }
            else
            {
                MessageBox.Show("Неправильна вершина. Будь ласка, введіть правильне ціле число.");
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

                Dictionary<int, int> H_dic = new Dictionary<int, int>();
                Dictionary<int, int> H_Bad_dic = new Dictionary<int, int>();


                string[] lines = InputTextBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    ar.Add(line);
                }

                foreach (var i in ar)
                {
                    Console.WriteLine(i);
                    var parts = i.Split(" ");
                    string key = parts[0];
                    int value = int.Parse(parts[1]);
                    dictionary[key] = value;
                    inverse[key] = 0;
                }

                int n = int.Parse(EndVertexTextBox.Text);

                List<int> result = new List<int>();
                List<string> our_key = new List<string>();

                int start = 1;
                List<string> t = new List<string>();
                List<int> max_val = new List<int>();
                List<string> r = new List<string>();
                bool status = false;
                List<List<string>> ways = new List<List<string>>();
                List<List<string>> bad = new List<List<string>>();

                H_dic.Clear();
                H_Bad_dic.Clear();

                List<int> st = new List<int>();
                List<int> min_r = new List<int>();
                bool need_to_continue = false;

                List<string> completed = new List<string>();
                List<string> possible_ways = new List<string>();
                List<string> remember = new List<string>();
                bool pos_w = false;
                int last_el = 0;
                List<int> g = new List<int>();

                foreach (var i in dictionary)
                {
                    var parts = i.Key.Split("-");
                    int el1 = int.Parse(parts[0]);
                    int el2 = int.Parse(parts[1]);
                    if (el2 == n)
                    {
                        possible_ways.Add(i.Key);
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

                string skip_way = "";

                while (!status)
                {
                    if (string.IsNullOrEmpty(skip_way))
                    {
                        t.Clear();
                    }
                    max_val.Clear();
                    List<string> repeated = new List<string>();
                    bool second_cicle = false;

                    while (start != n)
                    {
                        List<int> x = new List<int>();
                        our_key.Clear();

                        foreach (var i in dictionary)
                        {
                            var parts = i.Key.Split("-");
                            int el1 = int.Parse(parts[0]);
                            int el2 = int.Parse(parts[1]);

                            if (pos_w)
                            {
                                break;
                            }

                            if (start == el1)
                            {
                                if (repeated.Contains(i.Key))
                                {
                                    if (H_dic[el1] - H_Bad_dic[el1] != 1)
                                    {
                                        skip_way = t[t.IndexOf(i.Key)];
                                    }
                                    else
                                    {
                                        skip_way = t[t.IndexOf(i.Key) + 1];
                                        t.RemoveRange(t.IndexOf(i.Key) + 1, t.Count - t.IndexOf(i.Key) - 1);
                                        start = el2;
                                        second_cicle = true;
                                        break;
                                    }
                                }

                                if (n != last_el && (el2 == g[g.Count - 1] || (possible_ways.Contains(i.Key) && !remember.Contains(i.Key))))
                                {
                                    if (possible_ways.Contains(i.Key))
                                    {
                                        completed.Add(i.Key);
                                    }
                                    our_key.Clear();
                                    our_key.Add(i.Key);
                                    x.Clear();
                                    int sub = dictionary[i.Key] - inverse[i.Key];
                                    x.Add(sub);
                                    pos_w = true;
                                    break;
                                }

                                if (!st.Contains(el2) && i.Key != skip_way)
                                {
                                    our_key.Add(i.Key);
                                    int sub = dictionary[i.Key] - inverse[i.Key];

                                    if (sub == 0)
                                    {
                                        bad.Add(t);
                                        if (!r.Contains(i.Key))
                                        {
                                            r.Add(i.Key);
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
                        if (x.Max() == 0)
                        {
                            break;
                        }

                        max_val.Add(x.Max());
                        int ind = x.IndexOf(x.Max());
                        t.Add(our_key[ind]);
                        repeated.Add(our_key[ind]);

                        var el_parts = our_key[ind].Split("-");
                        int el1_new = int.Parse(el_parts[0]);
                        int el2_new = int.Parse(el_parts[1]);
                        start = el2_new;
                    }

                    if (!second_cicle)
                    {
                        skip_way = "";
                        int min_val = max_val.Min();
                        ways.Add(new List<string>(t));
                        min_r.Add(min_val);

                        foreach (var i in t)
                        {
                            inverse[i] += min_val;
                        }

                        foreach (var i in dictionary)
                        {
                            int sub = dictionary[i.Key] - inverse[i.Key];

                            if (sub == 0)
                            {
                                bad.Add(t);
                                if (!r.Contains(i.Key))
                                {
                                    r.Add(i.Key);
                                }
                            }
                        }

                        int count = 0;
                        foreach (var i in possible_ways)
                        {
                            foreach (var j in r)
                            {
                                if (i == j)
                                {
                                    count++;
                                    if (!remember.Contains(i))
                                    {
                                        remember.Add(i);
                                    }
                                }
                            }
                        }
                        var firstNotSecond = completed.Except(possible_ways).ToList();
                        var secondNotFirst = possible_ways.Except(completed).ToList();
                        if ((!firstNotSecond.Any() && !secondNotFirst.Any()) || count == possible_ways.Count)
                        {
                            status = true;
                            break;
                        }

                        start = 1;
                        int n1 = 1;
                        while (n1 != last_el + 1)
                        {
                            int k1 = 0;
                            foreach (var i in dictionary)
                            {
                                var parts = i.Key.Split("-");
                                int el1 = int.Parse(parts[0]);
                                int el2 = int.Parse(parts[1]);
                                if (el1 == n1)
                                {
                                    k1++;
                                }
                            }
                            H_dic[n1] = k1;
                            n1++;
                        }

                        for (int i = 1; i <= last_el; i++)
                        {
                            H_Bad_dic[i] = 0;
                        }

                        foreach (var i in dictionary)
                        {
                            if (r.Contains(i.Key))
                            {
                                var el_parts = i.Key.Split("-");
                                int el1 = int.Parse(el_parts[0]);
                                H_Bad_dic[el1]++;
                            }
                        }

                        int numb = 0;
                        List<int> check = new List<int>();
                        foreach (var i in r)
                        {
                            var el_parts = i.Split("-");
                            int el1 = int.Parse(el_parts[0]);
                            int el2 = int.Parse(el_parts[1]);
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
                                var el_1_parts = j.Key.Split("-");
                                int el_1 = int.Parse(el_1_parts[0]);
                                int el_2 = int.Parse(el_1_parts[1]);
                                if (el1 == el_2)
                                {
                                    numb++;
                                    if (!check.Contains(el_1))
                                    {
                                        check.Add(el_1);
                                    }
                                }
                            }
                        }

                        bool need_to_stop = false;
                        if (check.Contains(1))
                        {
                            check.Remove(1);
                        }
                        if (check.Count == 0)
                        {
                            need_to_stop = true;
                        }
                        while (!need_to_stop)
                        {
                            var toRemove = new List<int>(); // List to store items to remove

                            foreach (var i in new List<int>(check)) // Iterate over a copy of check
                            {
                                foreach (var j in dictionary)
                                {
                                    var el_1_parts = j.Key.Split("-");
                                    int el_1 = int.Parse(el_1_parts[0]);
                                    int el_2 = int.Parse(el_1_parts[1]);
                                    if (i == el_2)
                                    {
                                        numb++;
                                        if (!check.Contains(el_1))
                                        {
                                            check.Add(el_1);
                                        }
                                    }
                                }
                                toRemove.Add(i); // Add to remove list
                            }

                            // Now remove items from check
                            foreach (var item in toRemove)
                            {
                                check.Remove(item);
                            }

                            if (check.Contains(1))
                            {
                                need_to_stop = true;
                            }
                        }

                        bool one_cicle = false;
                        st.Clear();
                        List<int> use = new List<int>();

                        if (numb == 0)
                        {
                            numb = 2;
                        }

                        for (int k = 0; k < numb; k++)
                        {
                            if (status)
                            {
                                break;
                            }
                            if (one_cicle)
                            {
                                foreach (var i in dictionary)
                                {
                                    var el1_parts = i.Key.Split("-");
                                    int el1 = int.Parse(el1_parts[0]);
                                    int el2 = int.Parse(el1_parts[1]);
                                    if (H_Bad_dic[el2] == H_dic[el2] && H_Bad_dic[el2] != 0 && !st.Contains(el2) && !r.Contains(i.Key))
                                    {
                                        H_Bad_dic[el1]++;
                                        if (!st.Contains(el2))
                                        {
                                            st.Add(el2);
                                        }
                                        if (!use.Contains(el1))
                                        {
                                            use.Add(el1);
                                        }
                                    }
                                }
                            }

                            foreach (var i in dictionary)
                            {
                                var el1_parts = i.Key.Split("-");
                                int el1 = int.Parse(el1_parts[0]);
                                int el2 = int.Parse(el1_parts[1]);
                                if (H_Bad_dic[el2] == H_dic[el2] && H_Bad_dic[el2] != 0 && !use.Contains(el1) && !r.Contains(i.Key))
                                {
                                    H_Bad_dic[el1]++;
                                    if (!st.Contains(el2))
                                    {
                                        st.Add(el2);
                                    }
                                    if (!use.Contains(el1))
                                    {
                                        use.Add(el1);
                                    }
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

                            one_cicle = true;
                            need_to_continue = false;
                        }
                    }
                }
                DrawGraph();
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
                MinimumCrossSection.Text = "Максимальний потік: " + min_r.Sum() + "\n";
                MinimumCrossSection.Text += "Мінімальні перерізи: \n";
                foreach (var v in ways)
                {
                    MinimumCrossSection.Text += string.Join(", ", v) + "\n";
                }
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
