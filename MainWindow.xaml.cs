namespace GPUImageViewer
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using BitMiracle.LibTiff.Classic;

    public partial class MainWindow : Window
    {

        // 原始bigtiff的高宽
        private int bigTiffHeight;

        private int bigTiffWidth;

        private bool dragging;

        private string filePath;

        // 读入预览影像宽/真实影像宽
        private double imageZoom;

        private Point old_pos;

        // 当前展示的图片区域在原始影像的位置，左右上下
        private double x0;

        private double x1;

        private double y0;

        private double y1;

        // 当前的缩放尺度
        private double zoom = 1;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.dragging)
            {
                this.dragging = true;
                this.old_pos = e.GetPosition(this.grid);
            }
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.dragging = false;
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.dragging)
            {
                Point new_pos = e.GetPosition(this.grid);
                double deltax = new_pos.X - this.old_pos.X;
                double deltay = new_pos.Y - this.old_pos.Y;
                deltax /= this.grid.ActualWidth;
                deltay /= this.grid.ActualHeight;
                deltax *= this.x1 - this.x0;
                deltay *= this.y1 - this.y0;
                this.x0 -= deltax;
                this.x1 -= deltax;
                this.y0 -= deltay;
                this.y1 -= deltay;

                // 防止读取的区域大于总区域
                if (this.x0 < 0)
                {
                    this.x1 = this.x1 - this.x0;
                    this.x0 = 0;
                }

                if (this.y0 < 0)
                {
                    this.y1 = this.y1 - this.y0;
                    this.y0 = 0;
                }

                if (this.x1 > this.bigTiffWidth)
                {
                    this.x0 = this.x0 - (this.x1 - this.bigTiffWidth);
                    this.x1 = this.bigTiffWidth;
                }

                if (this.y1 > this.bigTiffHeight)
                {
                    this.y0 = this.y0 - (this.y1 - this.bigTiffHeight);
                    this.y1 = this.bigTiffHeight;
                }

                this.image.Source = this.readRectFromBigTiff(
                    this.filePath,
                    Convert.ToInt32(this.x0),
                    Convert.ToInt32(this.x1),
                    Convert.ToInt32(this.y0),
                    Convert.ToInt32(this.y1),
                    this.grid.ActualWidth,
                    this.grid.ActualHeight);
                this.old_pos = new_pos;
            }
        }

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var point = Mouse.GetPosition(this.grid);

            // 大于1表示要放大
            var zoom = e.Delta > 0 ? 1.1 : 1 / 1.1;
            this.zoom *= zoom;

            // 想要缩小，即展示的区域中会出现空白区域，这个不支持
            if (this.zoom < 1)
            {
                this.zoom = 1;
            }
            else
            {
                // 鼠标位置在grid中的百分比
                double leftRatio = point.X / this.grid.ActualWidth;
                double topRatio = point.Y / this.grid.ActualHeight;

                // 鼠标位置在图像中的位置
                double xCenter = this.x0 + (this.x1 - this.x0) * leftRatio;
                double yCenter = this.y0 + (this.y1 - this.y0) * topRatio;

                // 缩放后需要读入的图像的宽高
                double width = (this.x1 - this.x0) / zoom;
                double height = (this.y1 - this.y0) / zoom;

                // 缩放后需要读入的图像边界，保证鼠标对应图片位置缩放后不变
                this.x0 = xCenter - width * leftRatio;
                this.x1 = xCenter + width * (1 - leftRatio);
                this.y0 = yCenter - height * topRatio;
                this.y1 = yCenter + height * (1 - topRatio);

                // 防止读取的区域大于总区域
                if (this.x0 < 0)
                {
                    this.x1 = this.x1 - this.x0;
                    this.x0 = 0;
                }

                if (this.y0 < 0)
                {
                    this.y1 = this.y1 - this.y0;
                    this.y0 = 0;
                }

                if (this.x1 > this.bigTiffWidth)
                {
                    this.x0 = this.x0 - (this.x1 - this.bigTiffWidth);
                    this.x1 = this.bigTiffWidth;
                }

                if (this.y1 > this.bigTiffHeight)
                {
                    this.y0 = this.y0 - (this.y1 - this.bigTiffHeight);
                    this.y1 = this.bigTiffHeight;
                }

                this.image.Source = this.readRectFromBigTiff(
                    this.filePath,
                    Convert.ToInt32(this.x0),
                    Convert.ToInt32(this.x1),
                    Convert.ToInt32(this.y0),
                    Convert.ToInt32(this.y1),
                    this.grid.ActualWidth,
                    this.grid.ActualHeight);
            }
        }

        private void open_filename(string name)
        {
            this.filePath = name;

            using (Tiff bigTiff = Tiff.Open(name, "r"))
            {
                int originalImageWidth = bigTiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int originalImageHeight = bigTiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                this.bigTiffHeight = originalImageHeight;
                this.bigTiffWidth = originalImageWidth;
            }

            int x0 = 0;
            int y0 = 0;
            int y1 = this.bigTiffHeight;
            int x1 = Convert.ToInt32(y1 / this.grid.ActualHeight * this.grid.ActualWidth);

            this.x0 = x0;
            this.x1 = x1;
            this.y1 = y1;
            this.y0 = y0;

            // 第一次读进来，默认只读最左边进来，填满整个视图
            this.image.Source = this.readRectFromBigTiff(
                name,
                x0,
                x1,
                y0,
                y1,
                this.grid.ActualWidth,
                this.grid.ActualHeight);
            this.image.RenderTransform = new MatrixTransform();
        }

        /// <summary>
        /// 从一张大的tif读取一部分，当区域的宽高大于需要的宽高时会进行采样以进行更快地读取
        /// </summary>
        /// <param name="name"></param>
        /// <param name="x0"></param>
        /// <param name="x1"></param>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="desiredWidth"></param>
        /// <param name="desiredHeight"></param>
        /// <returns></returns>
        private BitmapSource readRectFromBigTiff(
            string name,
            int x0,
            int x1,
            int y0,
            int y1,
            double desiredWidth,
            double desiredHeight)
        {
            BitmapSource bitmapSource = new BitmapImage();
            int width = x1 - x0;
            int height = y1 - y0;

            using (Tiff bigTiff = Tiff.Open(name, "r"))
            {
                // 如果要的宽度高度已经小于框的宽高，则读所有数据
                if (width < desiredWidth && height < desiredHeight)
                {
                    byte[,] bufferRect = new byte[height, width];
                    byte[] buffer = new byte[this.bigTiffWidth];

                    for (int j = y0; j < y1; j += 1)
                    {
                        bigTiff.ReadScanline(buffer, j);
                        Buffer.BlockCopy(buffer, x0, bufferRect, (j - y0) * width * sizeof(byte), width * sizeof(byte));
                    }

                    byte[] bufferRectFlatten = new byte[height * width];
                    Buffer.BlockCopy(bufferRect, 0, bufferRectFlatten, 0, width * height * sizeof(byte));
                    bitmapSource = BitmapSource.Create(
                        width,
                        height,
                        96.0,
                        96.0,
                        PixelFormats.Gray8,
                        BitmapPalettes.Gray256,
                        bufferRectFlatten,
                        (width * 8 + 7) / 8);
                }
                else
                {
                    // Console.WriteLine($"{width},{desiredWidth}");
                    int skip = Convert.ToInt32(width / desiredWidth);

                    int resizedHeight = Convert.ToInt32(height / skip);
                    int resizedWidth = Convert.ToInt32(width / skip);

                    byte[,] bufferRect = new byte[resizedHeight, resizedWidth];
                    byte[] buffer = new byte[this.bigTiffWidth];
                    for (int j = y0; j + skip < y1; j += skip)
                    {
                        bigTiff.ReadScanline(buffer, j);
                        for (int i = x0; i + skip < x1; i += skip)
                        {
                            bufferRect[(j - y0) / skip, (i - x0) / skip] = buffer[i];
                        }
                    }

                    byte[] bufferRectFlatten = new byte[resizedHeight * resizedWidth];
                    Buffer.BlockCopy(bufferRect, 0, bufferRectFlatten, 0, resizedHeight * resizedWidth * sizeof(byte));
                    bitmapSource = BitmapSource.Create(
                        resizedWidth,
                        resizedHeight,
                        96.0,
                        96.0,
                        PixelFormats.Gray8,
                        BitmapPalettes.Gray256,
                        bufferRectFlatten,
                        (resizedWidth * 8 + 7) / 8);
                }
            }

            return bitmapSource;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.open_filename(files[0]);
        }
    }
}