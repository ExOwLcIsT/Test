using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Test
{
    public partial class VerticesWindow : Window
    {
        private Point mouseOffset;
        private string[] vertices;
        private double radius = 20; // Радіус кола
        private double spacing = 50; // Відстань між колами
        private double startX = 50; // Початкова координата X
        private double startY = 100; // Початкова координата Y

        private Dictionary<string, Ellipse> vertexCircles = new Dictionary<string, Ellipse>(); // Зберігаємо вершини та їхні круги
        private Dictionary<string, Line> edgeLines = new Dictionary<string, Line>(); // Зберігаємо ребра та їхні лінії
        private Dictionary<string, Path> arrows = new Dictionary<string, Path>(); // Зберігаємо стрілки для кожного ребра

        public VerticesWindow(string[] vertices, string[] edges, string[] shortestPathEdges)
        {
            InitializeComponent();
            this.vertices = vertices;
            DrawVertices(vertices);
            DrawEdges(edges, shortestPathEdges);
        }

        private void DrawVertices(string[] vertices)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                // Створення кола для вершини
                Ellipse circle = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                // Розміщення кола на канвасі
                Canvas.SetLeft(circle, startX + i * spacing);
                Canvas.SetTop(circle, startY);
                canvas.Children.Add(circle);

                // Додавання тексту в центр кола
                TextBlock textBlock = new TextBlock
                {
                    Text = vertices[i],
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                };

                // Додавання тексту на канвас
                Canvas.SetLeft(textBlock, startX + i * spacing + radius - 10); // Вирівнювання тексту
                Canvas.SetTop(textBlock, startY + radius - 10);
                canvas.Children.Add(textBlock);

                // Додати обробники подій для перетягування
                circle.MouseLeftButtonDown += Circle_MouseLeftButtonDown;
                circle.MouseMove += Circle_MouseMove;
                circle.MouseLeftButtonUp += Circle_MouseLeftButtonUp;
                circle.Tag = textBlock; // Зберігаємо текстовий блок у тегу кола

                vertexCircles[vertices[i]] = circle; // Зберігаємо коло у словник для оновлення при переміщенні
            }
        }

        private void DrawEdges(string[] edges, string[] shortestPathEdges)
        {
            foreach (string edge in edges)
            {
                // Отримання інформації про з'єднання
                string[] nodes = edge.Split('-');
                string fromVertex = nodes[0];
                string toVertex = nodes[1];

                if (vertexCircles.ContainsKey(fromVertex) && vertexCircles.ContainsKey(toVertex))
                {
                    // Визначення кольору для лінії
                    SolidColorBrush lineColor = shortestPathEdges.Contains(edge) ? Brushes.Yellow : Brushes.Gray;

                    // Створення лінії
                    Line line = new Line
                    {
                        Stroke = lineColor,
                        StrokeThickness = 2,
                        X1 = Canvas.GetLeft(vertexCircles[fromVertex]) + radius,
                        Y1 = Canvas.GetTop(vertexCircles[fromVertex]) + radius,
                        X2 = Canvas.GetLeft(vertexCircles[toVertex]) + radius,
                        Y2 = Canvas.GetTop(vertexCircles[toVertex]) + radius
                    };

                    // Додавання лінії на канвас
                    canvas.Children.Add(line);
                    edgeLines[edge] = line; // Зберігаємо лінію у словник для динамічного оновлення

                    // Додавання стрілочки
                    Path arrow = CreateArrow(line.X1, line.Y1, line.X2, line.Y2);
                    canvas.Children.Add(arrow);
                    arrows[edge] = arrow; // Зберігаємо стрілку
                }
            }
        }

        private void Circle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse circle)
            {
                mouseOffset = e.GetPosition(circle);
                circle.CaptureMouse();
            }
        }

        private void Circle_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse circle && circle.IsMouseCaptured)
            {
                // Отримуємо нові координати
                Point position = e.GetPosition(canvas);
                Canvas.SetLeft(circle, position.X - mouseOffset.X);
                Canvas.SetTop(circle, position.Y - mouseOffset.Y);

                // Оновлюємо текстовий блок
                TextBlock textBlock = (TextBlock)circle.Tag;
                Canvas.SetLeft(textBlock, position.X - mouseOffset.X + 10);
                Canvas.SetTop(textBlock, position.Y - mouseOffset.Y + 10);

                // Оновлюємо лінії, пов'язані з цією вершиною
                UpdateEdges(circle);
            }
        }

        private void Circle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse circle)
            {
                circle.ReleaseMouseCapture();
            }
        }

        private void UpdateEdges(Ellipse movedCircle)
        {
            // Оновлюємо координати всіх ліній і стрілок, пов'язаних з цією вершиною
            foreach (var edge in edgeLines)
            {
                string[] nodes = edge.Key.Split('-');
                string fromVertex = nodes[0];
                string toVertex = nodes[1];

                // Якщо ця вершина входить у з'єднання, оновлюємо координати лінії
                if (vertexCircles[fromVertex] == movedCircle || vertexCircles[toVertex] == movedCircle)
                {
                    Line line = edge.Value;

                    if (vertexCircles.ContainsKey(fromVertex) && vertexCircles.ContainsKey(toVertex))
                    {
                        line.X1 = Canvas.GetLeft(vertexCircles[fromVertex]) + radius;
                        line.Y1 = Canvas.GetTop(vertexCircles[fromVertex]) + radius;
                        line.X2 = Canvas.GetLeft(vertexCircles[toVertex]) + radius;
                        line.Y2 = Canvas.GetTop(vertexCircles[toVertex]) + radius;

                        // Оновлюємо стрілку
                        Path arrow = arrows[edge.Key];
                        UpdateArrow(arrow, line.X1, line.Y1, line.X2, line.Y2, radius);
                    }
                }
            }
        }

        private Path CreateArrow(double x1, double y1, double x2, double y2)
        {
            // Визначаємо кут нахилу лінії
            double angle = Math.Atan2(y2 - y1, x2 - x1);

            // Розмір стрілочки
            double arrowSize = 10;

            // Координати кінця лінії (точка, де розміщуватиметься стрілка)
            Point endPoint = new Point(x2, y2);

            // Визначаємо координати трикутника (стрілки)
            Point arrowP1 = new Point(
                endPoint.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle - Math.PI / 6)
            );

            Point arrowP2 = new Point(
                endPoint.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle + Math.PI / 6)
            );

            // Створюємо трикутник (стрілку) як `Path`
            PathFigure arrowHead = new PathFigure { StartPoint = endPoint };
            arrowHead.Segments.Add(new LineSegment(arrowP1, true));
            arrowHead.Segments.Add(new LineSegment(arrowP2, true));
            arrowHead.Segments.Add(new LineSegment(endPoint, true));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(arrowHead);

            Path arrowPath = new Path
            {
                Stroke = Brushes.Black,
                Fill = Brushes.Black,
                StrokeThickness = 2,
                Data = geometry
            };

            return arrowPath;
        }

        private void UpdateArrow(Path arrow, double x1, double y1, double x2, double y2, double circleRadius)
        {
            double angle = Math.Atan2(y2 - y1, x2 - x1);
            double arrowSize = 10;

            Point endPoint = new Point(
                x2 - circleRadius * Math.Cos(angle),
                y2 - circleRadius * Math.Sin(angle)
            );

            Point arrowP1 = new Point(
                endPoint.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle - Math.PI / 6)
            );

            Point arrowP2 = new Point(
                endPoint.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle + Math.PI / 6)
            );

            PathFigure arrowHead = ((PathGeometry)arrow.Data).Figures.First();
            arrowHead.StartPoint = endPoint;
            arrowHead.Segments[0] = new LineSegment(arrowP1, true);
            arrowHead.Segments[1] = new LineSegment(arrowP2, true);
            arrowHead.Segments[2] = new LineSegment(endPoint, true);
        }
    }
}

