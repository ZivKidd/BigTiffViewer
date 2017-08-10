using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GPUImageViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = new TimeSpan(100000);
            timer.Tick += new EventHandler(handle_timer);
            timer.Start();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                open_filename(args[1]);

            grid.Background = brushes[brush_index];
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        }

        private string file_directory = null;
        private List<string> filenames = null;
        private int cursor = 0;

        enum PICSTARTMODE
        {
            FIT,
            ORIGINALTOP,
        };

        PICSTARTMODE startmode = PICSTARTMODE.FIT;

        Brush[] brushes = new Brush[] { new SolidColorBrush(Colors.White), new SolidColorBrush(Colors.Black) };
        int brush_index = 0;

        public static ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            open_filename(files[0]);
        }

        private void open_filename(string name)
        {
            image.Source = BitmapFromUri(new Uri(name));
            init_transform();
            file_directory = System.IO.Path.GetDirectoryName(name);
            var all_files = new List<string>(Directory.GetFiles(file_directory).OrderBy(f => f));

            var regex = new Regex(@".+\.(jpg|png|jpeg|bmp)", RegexOptions.IgnoreCase);

            filenames = all_files.Where(f => regex.IsMatch(f)).ToList();
            cursor = filenames.IndexOf(name);
        }

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(image);
            double zoom = e.Delta > 0 ? .2 : -.2;
            perform_zoom(p, zoom);
        }

        private void perform_zoom(Point p, double zoom)
        {
            var m = image.RenderTransform.Value;
            m.ScaleAtPrepend(1.0 + zoom, 1.0 + zoom, p.X, p.Y);
            image.RenderTransform = new MatrixTransform(m);
        }

        Point old_pos = new Point();
        bool dragging = false;

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point new_pos = e.GetPosition(this);
                double deltax = new_pos.X - old_pos.X;
                double deltay = new_pos.Y - old_pos.Y;
                var m = image.RenderTransform.Value;
                m.Translate(deltax, deltay);
                image.RenderTransform = new MatrixTransform(m);
                old_pos = new_pos;
            }
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!dragging)
            {
                dragging = true;
                old_pos = e.GetPosition(this);
            }
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    if (this.WindowState == WindowState.Maximized)
                    {
                        if (this.WindowStyle == WindowStyle.None)
                        {
                            this.WindowStyle = WindowStyle.SingleBorderWindow;
                            this.ResizeMode = ResizeMode.CanResize;
                            this.WindowState = WindowState.Normal;                            
                            this.WindowState = WindowState.Maximized;
                            Mouse.OverrideCursor = null;
                        }
                        else
                        {
                            this.WindowStyle = WindowStyle.None;
                            this.ResizeMode = ResizeMode.NoResize;
                            this.WindowState = WindowState.Normal;
                            this.WindowState = WindowState.Maximized;
                            Mouse.OverrideCursor = Cursors.None;
                        }
                    }
                    else
                    {
                        this.WindowStyle = WindowStyle.None;
                        this.ResizeMode = ResizeMode.NoResize;
                        this.WindowState = WindowState.Maximized;
                        Mouse.OverrideCursor = Cursors.None;
                    }
                    break;
                case Key.Escape:
                    init_transform();
                    break;
                case Key.X:
                    next_picture();
                    break;
                case Key.Z:
                    prev_picture();
                    break;
                case Key.Right:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || 
                        Keyboard.IsKeyDown(Key.RightShift))
                    {
                        next_picture();
                    }
                    break;
                case Key.Left:
                    if (Keyboard.IsKeyDown(Key.LeftShift) ||
                        Keyboard.IsKeyDown(Key.RightShift))
                    {
                        prev_picture();
                    }
                    break;
                case Key.V:
                    if (startmode == PICSTARTMODE.FIT)
                        startmode = PICSTARTMODE.ORIGINALTOP;
                    else if (startmode == PICSTARTMODE.ORIGINALTOP)
                        startmode = PICSTARTMODE.FIT;
                    break;
                case Key.B:
                    brush_index = (brush_index + 1 == brushes.Length) ? 0 : brush_index + 1;
                    grid.Background = brushes[brush_index];
                    break;
            }
        }

        private void next_picture()
        {
            if (filenames.Count > 0)
            {
                cursor = (cursor + 1) % filenames.Count;
                update_from_cursor();
            }
        }

        private void prev_picture()
        {
            if (filenames.Count > 0)
            {
                cursor--;
                if (cursor < 0)
                    cursor = filenames.Count - 1;
                update_from_cursor();
            }
        }

        private Point get_image_center_global()
        {
            var m = image.RenderTransform.Value;
            var im = m;
            im.Invert();
            Point p = im.Transform(new Point(image.ActualWidth / 2.0, image.ActualHeight / 2.0));
            return p;
        }

        DispatcherTimer timer = new DispatcherTimer();

        const double KEYBOARD_SPEED = 12.5;
        const double KEYBOARD_ZOOM_SPEED = 0.03;

        private void handle_timer(object sender, EventArgs args)
        {
            if (image.Source == null)
                return;
            if (!this.IsKeyboardFocusWithin)
                return;
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.A))
                {
                    var m = image.RenderTransform.Value;
                    m.Translate(KEYBOARD_SPEED, 0.0);
                    image.RenderTransform = new MatrixTransform(m);
                }
                if (Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.D))
                {
                    var m = image.RenderTransform.Value;
                    m.Translate(-KEYBOARD_SPEED, 0.0);
                    image.RenderTransform = new MatrixTransform(m);
                }
                if (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.W))
                {
                    var m = image.RenderTransform.Value;
                    m.Translate(0.0, KEYBOARD_SPEED);
                    image.RenderTransform = new MatrixTransform(m);
                }
                if (Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.S))
                {
                    var m = image.RenderTransform.Value;
                    m.Translate(0.0, -KEYBOARD_SPEED);
                    image.RenderTransform = new MatrixTransform(m);
                }
                if (Keyboard.IsKeyDown(Key.Q))
                {
                    Point p = get_image_center_global();
                    perform_zoom(p, -KEYBOARD_ZOOM_SPEED);
                }
                if (Keyboard.IsKeyDown(Key.E))
                {
                    Point p = get_image_center_global();
                    perform_zoom(p, KEYBOARD_ZOOM_SPEED);
                }
            }
        }

        private void update_from_cursor()
        {
            if (filenames.Count > 1)
            {
                string img_name = filenames[cursor];
                image.Source = BitmapFromUri(new Uri(img_name));
                init_transform();
            }
        }

        private void init_transform()
        {
            if (image.Source == null)
                return;
            switch (startmode)
            {
                case PICSTARTMODE.FIT:
                    image.RenderTransform = new MatrixTransform();
                    break;
                case PICSTARTMODE.ORIGINALTOP:
                    image.RenderTransform = new MatrixTransform();
                    double xscale = grid.ActualWidth / image.Source.Width;
                    double yscale = grid.ActualHeight / image.Source.Height;
                    double current_scale = Math.Min(xscale, yscale);
                    double k = 1.0;
                    if (xscale < 1.0)
                        k = xscale;
                    var trans = new MatrixTransform();
                    var m = trans.Value;
                    m.ScaleAtPrepend(k / current_scale, k / current_scale, 
                        current_scale * image.Source.Width / 2.0, 0.0);
                    image.RenderTransform = new MatrixTransform(m);
                    break;
            }
        }
    }
}
