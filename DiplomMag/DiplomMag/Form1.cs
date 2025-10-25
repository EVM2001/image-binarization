using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Contracts;

namespace DiplomMag
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string FilePath;
        string Filename;
        int width, height;//ширина и высота        
        Bitmap bitmap;//элемент класса для работы с пикселями        
        short[][] Colors;//массив цветов
        
        Chart ChartForHyst;

        byte[] File = null;//массив для записи файла
        byte[] widthBytes = new byte[4], heightBytes = new byte[4], ColorBytes = new byte[2];//ширина, высота и яркость пикселя
        int upString = 0;//верхняя строка
        short offset = 0;// сдвиг
        byte[] SaveFile = null;//массив для сохранения файла

        double[,] XMask = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };//для оператора Собеля
        double[,] YMask = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };//для оператора Собеля

        Bitmap OffsetBitmap;

        int BotBorder, TopBorder, SaveBotBorder, SaveTopBorder;

        Boolean IsBinarized = false;
        
        int[,] S_Element = {
            { 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1 }
        };//структурирующий элемент для морфологических операций

        Bitmap ReservedBitmap = null;

        short[][] NormalizedColors;
        Bitmap NormalizedBitmap;

        int[] Lights2;

        short view = 0;
        Bitmap HystBitmap;
        Bitmap NormalizedHystBitmap;
        Bitmap BinRegularBmap;
        Bitmap BinNormalizedBmap;

        //реализация кнопки "загрузить"
        private void GetAndShowFile(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                
                FilePath = openFileDialog1.FileName;//задание пути к файлу
                Filename = openFileDialog1.SafeFileName;//задание имени файла
                File = System.IO.File.ReadAllBytes(FilePath);                
                FileToArrays(File);
                ArraysToImage();
                txtFileName.Text = Filename;
                txtImageSize.Text = width.ToString() + " X " + height.ToString();

                IsBinarized = false;
                ReservedBitmap = null;
            }

        }

        //функция преобразующая массив данных в ширину, высоту и массив цветов (ширина и высота - 4 байта)
        private void FileToArrays(byte[] File)
        {            
            Array.Copy(File, widthBytes, 4);
            Array.Copy(File, 4, heightBytes, 0, 4);
            width = BitConverter.ToInt32(widthBytes, 0);
            height = BitConverter.ToInt32(heightBytes, 0);

            TopBorder = 0;
            BotBorder = height - 1;
            textBox1.Text = TopBorder.ToString();
            textBox2.Text = BotBorder.ToString();

            SaveTopBorder = 0;
            SaveBotBorder = height - 1;
            textBox3.Text = SaveTopBorder.ToString();
            textBox4.Text = SaveBotBorder.ToString();

            if (width > 6104 | height > 100000)
            {
                MessageBox.Show($"изображение слишком большое");
                return;
            }
            Colors = new short[height][];//заполнение массива цветов
            for (int i = 0; i < height; i++)
            {
                Colors[i] = new short[width];
            }
            int bytecount = 8;
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    Array.Copy(File, bytecount, ColorBytes, 0, 2);
                    Colors[h][w] = BitConverter.ToInt16(ColorBytes, 0);
                    bytecount += 2;
                }
            }
        }

        //функция преобразующая по имеющимся высоте и ширине массив цветов в изображение
        private void ArraysToImage()
        {
            bitmap = new Bitmap(width, height);            
            short FreeColors;

            OffsetBitmap = new Bitmap(width, height);
            short OffsetColors;

            short NormalizedOffsetColors;

            progressBar1.Value = 0;
            progressBar1.Maximum = height;
            progressBar1.Step = 1;

            NormalizedColors = Normalization(Colors, height, width, 1023, 0);//массив яркостей полученный нормализацией исходного массива яркостей к [0;1023]
            NormalizedBitmap = new Bitmap(width, height);

            for (int h = upString; h < height; h++)
            {                
                for (int w = 0; w < width; w++)
                {
                    FreeColors = Colors[h][w];
                    OffsetColors = Colors[h][w];
                    OffsetColors >>= offset;

                    

                    if (FreeColors < 256)//обработка исключения и задание пикселям цветов с учетом верхних строк
                    {
                        bitmap.SetPixel(w, h - upString, Color.FromArgb(FreeColors, FreeColors, FreeColors));
                        OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors, OffsetColors, OffsetColors));
                    }
                    else
                    {
                        bitmap.SetPixel(w, h - upString, Color.FromArgb(FreeColors % 256, FreeColors % 256, FreeColors % 256));
                        OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors % 256, OffsetColors % 256, OffsetColors % 256));
                    }

                    NormalizedOffsetColors = NormalizedColors[h][w];
                    NormalizedOffsetColors >>= offset;

                    if (NormalizedColors[h][w] < 256)//обработка исключения и задание пикселям цветов с учетом верхних строк
                    {
                        NormalizedBitmap.SetPixel(w, h - upString, Color.FromArgb(NormalizedOffsetColors, NormalizedOffsetColors, NormalizedOffsetColors));
                    }
                    else
                    {
                        NormalizedBitmap.SetPixel(w, h - upString, Color.FromArgb(NormalizedOffsetColors % 256, NormalizedOffsetColors % 256, NormalizedOffsetColors % 256));
                    }

                    /*if (NormalizedColors[h][w] < 256)//обработка исключения и задание пикселям цветов с учетом верхних строк
                    {
                        NormalizedBitmap.SetPixel(w, h - upString, Color.FromArgb(NormalizedColors[h][w], NormalizedColors[h][w], NormalizedColors[h][w]));
                    }
                    else
                    {
                        NormalizedBitmap.SetPixel(w, h - upString, Color.FromArgb(NormalizedColors[h][w] % 256, NormalizedColors[h][w] % 256, NormalizedColors[h][w] % 256));
                    }*/

                }
                progressBar1.PerformStep();
            }
            pictureBox1.Height = height - upString;//задание высоты контейнера для изображения
            pictureBox1.Width = width;//задание ширины контейнера для изображения

            NormalizedHystBitmap = CreateHystograme(NormalizedBitmap, pictureBox4, NormalizedColors);
            HystBitmap = CreateHystograme(bitmap, pictureBox4, Colors);
            if (checkBox1.Checked) 
            {
                pictureBox1.Image = NormalizedBitmap;                
                pictureBox4.Image = NormalizedHystBitmap;
            }
            else 
            {
                pictureBox1.Image = OffsetBitmap;
                pictureBox4.Image = HystBitmap;
            }
            view = 0;
        }

        private void SaveImage(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = txtFileName.Text;
            Bitmap Tmpbitmap = new Bitmap(pictureBox1.Image);

            SaveFile = new byte[(SaveBotBorder - SaveTopBorder + 1) * Tmpbitmap.Width * 2 + 8];

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    String FilePath = saveFileDialog1.FileName;//задание пути к файлу
                    if (pictureBox1.Image != null)
                    {
                        progressBar1.Value = 0;
                        progressBar1.Maximum = Tmpbitmap.Height;
                        progressBar1.Step = 1;

                        Array.Copy(widthBytes, 0, SaveFile, 0, 4);
                        Array.Copy(BitConverter.GetBytes(SaveBotBorder - SaveTopBorder + 1), 0, SaveFile, 4, 4);

                        int bytecounter = 8;
                        int Filebytecounter = 8;

                        for (int h = 0; h < Tmpbitmap.Height; h++)
                        {
                            for (int w = 0; w < Tmpbitmap.Width; w++)
                            {
                                if (h <= SaveBotBorder && h >= SaveTopBorder) 
                                {
                                    if (h <= BotBorder && h >= TopBorder && IsBinarized)
                                    {
                                        Array.Copy(BitConverter.GetBytes(Tmpbitmap.GetPixel(w, h).R), 0, SaveFile, bytecounter, 2);
                                        bytecounter += 2;
                                    }
                                    else
                                    {
                                        Array.Copy(File, Filebytecounter, SaveFile, bytecounter, 2);
                                        bytecounter += 2;
                                    }
                                }
                                Filebytecounter += 2;
                            }
                            progressBar1.PerformStep();
                        }

                        System.IO.File.WriteAllBytes(FilePath, SaveFile);

                    }
                    else MessageBox.Show("Ошибка");
                }
                catch
                {
                    MessageBox.Show("Ошибка");
                }
            }
        }

        Bitmap CreateHystograme(Bitmap bitmap, PictureBox picbox, short[][] Colors)
        {            
            int max2 = 0;
            Bitmap mybitmap = new Bitmap(picbox.Width, picbox.Height);
            Chart Graphic = new Chart();
            Graphic.Width = picbox.Width;
            Graphic.Height = picbox.Height;

            Graphic.ChartAreas.Add(new ChartArea("Hystograme"));
            Graphic.ChartAreas[0].AxisX.Title = "Код яркости пикселя";
            Graphic.ChartAreas[0].AxisY.Title = "Число пикселей";

            Graphic.ChartAreas[0].AxisX.Minimum = 0;
            Graphic.ChartAreas[0].AxisX.Maximum = 1024;            
            Graphic.ChartAreas[0].AxisX.Interval = 128;

            Series Hystograme = Graphic.Series.Add("Hystograme");
            Hystograme.ChartType = SeriesChartType.Column;
            

            Lights2 = new int[1024];

            int i, j;
            for (i = 0; i < bitmap.Width; i++)//проходим по всему изображению 
            {
                for (j = 0; j < bitmap.Height; j++)
                {
                    Lights2[Colors[j][i]]++;//заполняем массив, каждый элемент которого это количество пикселей с яркостью равной индексу массива
                }
            }

            for (i = 0; i < 1024; i++)//поиск максимального элемента
            {
                if (Lights2[i] > max2) max2 = Lights2[i];
            }

            Graphic.ChartAreas[0].AxisY.Maximum = max2;           
            for (i = 0; i < 1024; i++)//прорисовка гистограммы
            {
                if (Lights2[i] != 0)
                    for (j = 0; j <= Lights2[i]; j += Lights2[i])// отрисовываем столбец за столбцом нашу гистограмму 
                    {
                        Hystograme.Points.AddXY(i, j);
                    }
            }

            Graphic.DrawToBitmap(mybitmap, new Rectangle(0, 0, picbox.Width, picbox.Height));

            ChartForHyst = Graphic;

            return mybitmap;
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);
            
            short FreeColors;
            Bitmap newbitmap = new Bitmap(width, height);

            progressBar1.Value = 0;
            progressBar1.Maximum = height;
            progressBar1.Step = 1;
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        if (checkBox1.Checked)
                        {
                            FreeColors = NormalizedColors[h][w];//яркость из нормализованного к [0,1023] массива
                        }
                        else
                        {
                            FreeColors = Colors[h][w];//яркость из исходного файла           
                        }

                        if (FreeColors >= trackBar1.Value)
                        {
                            newbitmap.SetPixel(w, h, Color.FromArgb(255, 255, 255));//пиксель с яркостью равной или больше числового значения движка становится равным 255
                        }
                        else if (FreeColors < trackBar1.Value)
                        {
                            newbitmap.SetPixel(w, h, Color.FromArgb(0, 0, 0));//пиксель с яркостью меньше числового значения движка становится равным 0
                        }
                    }
                    else 
                    {
                        if (checkBox1.Checked)
                        {
                            newbitmap.SetPixel(w, h, NormalizedBitmap.GetPixel(w, h));
                        }
                        else
                        {
                            newbitmap.SetPixel(w, h, bitmap.GetPixel(w, h));
                        }
                    }          
                }
                progressBar1.PerformStep();
            }
            pictureBox1.Image = newbitmap;
            IsBinarized = true;

            //отрисовка красной линии ползунка на гистограмме
            Bitmap mybitmap = new Bitmap(pictureBox4.Width, pictureBox4.Height);
            Series VerticalStick = ChartForHyst.Series.Add("VerticalStick");
            VerticalStick.Color = Color.Red;
            VerticalStick.ChartType = SeriesChartType.Column;
            for (int j = 0; j < ChartForHyst.ChartAreas[0].AxisY.Maximum; j++)
            {
                VerticalStick.Points.AddXY(trackBar1.Value, j);
            }
            ChartForHyst.DrawToBitmap(mybitmap, new Rectangle(0, 0, pictureBox4.Width, pictureBox4.Height));

            pictureBox4.Image = mybitmap;
            ChartForHyst.Series.RemoveAt(1);

        }

        //реализация кнопок "сдвигать коды на"
        private void MoveCodeBy0(object sender, EventArgs e)//задание 0 величины сдвига вправо
        {
            offset = 0;
            if (File != null)
            {
                ArraysToImage();
            }
        }
        private void MoveCodeBy1(object sender, EventArgs e)//задание 1 величины сдвига вправо
        {
            offset = 1;            
            if (File != null)
            {
                ArraysToImage();
            }
        }
        private void MoveCodeBy2(object sender, EventArgs e)//задание 2 величины сдвига вправо
        {
            offset = 2;            
            if (File != null)
            {
                ArraysToImage();
            }
        }

        private void Binarization1_Click(object sender, EventArgs e)//нажатие на кнопку "Бернсен"
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = Binarization(NormalizedBitmap, 5, NormalizedColors);
            BinRegularBmap = Binarization(bitmap, 5, Colors);

            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }

            view = 1;

            IsBinarized = true;
        }

        //функция, реализующая бинаризацию(метод Бернсена)
        private Bitmap Binarization(Bitmap Sourcebitmap, int WindowSize, short[][] SourceColors)
        {
            Bitmap BinarizedImage = new Bitmap(width, height);
            int offset = (WindowSize - 1) / 2;
            progressBar1.Value = 0;
            progressBar1.Maximum = height - offset - offset;
            progressBar1.Step = 1;

            for (int h = offset; h < height - offset; h++)
            {
                for (int w = offset; w < width - offset; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        int T = (int)ForBinarization1(w, h, offset, Sourcebitmap, WindowSize, SourceColors);
                        if (SourceColors[h][w] < T) BinarizedImage.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                        else BinarizedImage.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else BinarizedImage.SetPixel(w, h, Sourcebitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }
            return BinarizedImage;
        }

        //функция для "подвижного окна"
        private double ForBinarization1(int w, int h, int offset, Bitmap Sourcebitmap, int WindowSize, short[][] SourceColors)
        {
            int max = SourceColors[h][w], min = SourceColors[h][w];

            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    if (SourceColors[i][j] > max) max = SourceColors[i][j];
                    if (SourceColors[i][j] < min) min = SourceColors[i][j];
                }
            }
            return (max + min) * 0.5;
        }

        private void Binarization2_Click(object sender, EventArgs e)//нажатие на кнопку "Брэдли"
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = bradley(NormalizedBitmap, 5, NormalizedColors);
            BinRegularBmap = bradley(bitmap, 5, Colors);            
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 2;

            IsBinarized = true;
        }

        private Bitmap bradley(Bitmap Sourcebitmap, int WindowSize, short[][] SourceColors)//Метод Брэдли
        {            
            Bitmap BinarizedImage = new Bitmap(width, height);

            int avg2 = 0;
            int avg = 0;
            int d = WindowSize;
            int offset = (d - 1) / 2;

            progressBar1.Value = 0;
            progressBar1.Maximum = width - offset - offset;
            progressBar1.Step = 1;

            for (int row = offset; row < width - offset; row++)
            {
                for (int column = offset; column < height - offset; column++)
                {
                    if (column <= BotBorder && column >= TopBorder)
                    {
                        for (int i = 0; i < d; i++)
                        {
                            for (int j = 0; j < d; j++)
                            {
                                avg += SourceColors[column + j - offset][row + i - offset];
                            }
                        }

                        avg2 = avg / (d * d);

                        float t = (avg2 * 15) / 100;
                        float lm = avg2 + t;
                        
                        int tmp = SourceColors[column][row];

                        if (tmp < lm)
                        {
                            BinarizedImage.SetPixel(row, column, Color.FromArgb(0, 0, 0));
                        }
                        else
                        {
                            BinarizedImage.SetPixel(row, column, Color.FromArgb(255, 255, 255));
                        }

                        avg = 0;
                    }
                    else BinarizedImage.SetPixel(row, column, Sourcebitmap.GetPixel(row, column));                   
                }
                progressBar1.PerformStep();
            }
            return BinarizedImage;
        }

        private void Binarization3_Click(object sender, EventArgs e)//нажатие на кнопку "Брэдли2"
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = BinarizationTest(NormalizedBitmap, NormalizedColors);
            BinRegularBmap = BinarizationTest(bitmap, Colors);
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 3;

            IsBinarized = true;
        }

        private Bitmap BinarizationTest(Bitmap Sourcebitmap, short[][] SourceColors)//Бинаризация3(Метод Брэдли2)
        {
            int d = width / 8;
            int Wcount = width / d;
            int Hcount = height / d;
            int[][] T;
            int count = d * d;
            int sum = 0;
            Bitmap BinarizedImage = new Bitmap(width, height);

            progressBar1.Value = 0;
            progressBar1.Maximum = Hcount;
            progressBar1.Step = 1;

            T = new int[Hcount][];//заполнение массива
            for (int i = 0; i < Hcount; i++)
            {
                T[i] = new int[Wcount];
            }

            for (int i = 0; i < Hcount; i++) 
            { 
                for (int j = 0; j < Wcount; j++) 
                {
                    for (int h = 0; h < d; h++)
                    {
                        for (int w = 0; w < d; w++)
                        {
                            sum += SourceColors[h + d * i][w + d * j];
                        }
                    }
                    int avg = sum / count;
                    int t = (int)(avg * 0.15);
                    T[i][j] = avg + t;
                    sum = 0;
                }
            }

            for (int i = 0; i < Hcount; i++)
            {
                for (int j = 0; j < Wcount; j++)
                {
                    for (int h = 0; h < d; h++)
                    {
                        for (int w = 0; w < d; w++)
                        {
                            int x = w + d * j;
                            int y = h + d * i;

                            if (y <= BotBorder && y >= TopBorder) 
                            {
                                if (SourceColors[y][x] < T[i][j])
                                {
                                    BinarizedImage.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                                }
                                else
                                {
                                    BinarizedImage.SetPixel(x, y, Color.FromArgb(255, 255, 255));
                                }
                            }
                            else BinarizedImage.SetPixel(x, y, Sourcebitmap.GetPixel(x, y));                            
                        }                        
                    }
                }
                progressBar1.PerformStep();
            }

            return BinarizedImage;
        }

        private void button6_Click(object sender, EventArgs e)//Оператор Собеля
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            Bitmap Tmpbitmap = new Bitmap(pictureBox1.Image);
            pictureBox1.Image = Sobel(Tmpbitmap);

            IsBinarized = true;
        }

        private Bitmap Sobel(Bitmap Sourcebitmap)
        {
            Bitmap FilteredImage = new Bitmap(width, height);
            int offset = 1;

            progressBar1.Value = 0;
            progressBar1.Maximum = height - offset - offset;
            progressBar1.Step = 1;

            for (int h = offset; h < height - offset; h++)
            {
                for (int w = offset; w < width - offset; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        int color = (int)ForSobel(w, h, XMask, YMask, offset, Sourcebitmap);

                        if (color >= 256) FilteredImage.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                        else FilteredImage.SetPixel(w, h, Color.FromArgb(color, color, color));
                    }
                    else FilteredImage.SetPixel(w, h, Sourcebitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }
            return FilteredImage;
        }
        private double ForSobel(int w, int h, double[,] Xmask, double[,] Ymask, int offset, Bitmap Sourcebitmap)
        {
            double newcolor = 0;
            double Gx = 0, Gy = 0;

            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    Gx += Xmask[i - (h - offset), j - (w - offset)] * Sourcebitmap.GetPixel(j, i).R;
                    Gy += Ymask[i - (h - offset), j - (w - offset)] * Sourcebitmap.GetPixel(j, i).R;
                }
            }
            newcolor = Math.Sqrt(Gx * Gx + Gy * Gy);
            return newcolor;
        }

        private void Binarization4_Click(object sender, EventArgs e)//Бинаризация4(метод Ниблэка)
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = Niblack(NormalizedBitmap, 5, NormalizedColors);
            BinRegularBmap = Niblack(bitmap, 5, Colors);
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 4;

            IsBinarized = true;
        }

        private Bitmap Niblack(Bitmap Sourcebitmap, int WindowSize, short[][] SourceColors)
        {
            Bitmap BinarizedImage = new Bitmap(width, height);
            int offset = (WindowSize - 1) / 2;

            progressBar1.Value = 0;
            progressBar1.Maximum = height - offset - offset;
            progressBar1.Step = 1;

            for (int h = offset; h < height - offset; h++)
            {
                for (int w = offset; w < width - offset; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        double mean = ForNiblack(w, h, offset, Sourcebitmap, WindowSize, SourceColors);
                        int T = (int)ForNiblack2(w, h, offset, Sourcebitmap, WindowSize, mean, SourceColors);
                        if (SourceColors[h][w] < T) BinarizedImage.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                        else BinarizedImage.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else BinarizedImage.SetPixel(w, h, Sourcebitmap.GetPixel(w, h));
                    
                }
                progressBar1.PerformStep();
            }

            return BinarizedImage;
        }

        private double ForNiblack(int w, int h, int offset, Bitmap Sourcebitmap, int WindowSize, short[][] SourceColors)
        {
            double newcolor = 0;
            int count = WindowSize * WindowSize;

            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    newcolor += SourceColors[i][j];
                }
            }
            return newcolor / count;
        }

        private double ForNiblack2(int w, int h, int offset, Bitmap Sourcebitmap, int WindowSize, double mean, short[][] SourceColors)
        {
            double res = 0;
            int count = WindowSize * WindowSize;
            double k = -0.2;
            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    double temp = SourceColors[i][j] - mean;
                    res += temp * temp;
                }
            }
            double s = Math.Sqrt(res / (count - 1));
            double T = mean + k * s;
            return T;
        }

        private void SetTopBorder(object sender, EventArgs e)//задание верхней границы бинаризации
        {
            int flag;
            if (textBox1.Text.Length == 0)
            {                
                textBox1.Text = "0";
            }
            else if (Int32.TryParse(textBox1.Text, out flag) == false)//если пользователь попробует ввести нечисловое значение
            {
                textBox1.Text = "0";
                MessageBox.Show($"введите числовое значение Верхней границы");
            }
            else TopBorder = Int32.Parse(textBox1.Text);
        }

        private void SetBotBorder(object sender, EventArgs e)//задание нижней границы бинаризации
        {
            int flag;
            if (textBox2.Text.Length == 0)
            {
                textBox2.Text = "0";
            }
            else if (Int32.TryParse(textBox2.Text, out flag) == false)//если пользователь попробует ввести нечисловое значение
            {
                textBox2.Text = "0";
                MessageBox.Show($"введите числовое значение Нижней границы");
            }
            else BotBorder = Int32.Parse(textBox2.Text);
        }

        private void Coordinates(object sender, MouseEventArgs e)//координаты курсора
        {
            if (File != null)
            {
                Xcoordinate.Text = e.X.ToString();
                Ycoordinate.Text = e.Y.ToString();
            }
        }

        private void Binarization5_Click(object sender, EventArgs e)//Бинаризация5(метод Отцу)
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = Otsu(NormalizedBitmap, NormalizedColors);
            BinRegularBmap = Otsu(bitmap, Colors);
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 5;

            IsBinarized = true;
        }        

        private Bitmap Otsu(Bitmap SourceBitmap, short[][] SourceColors)
        {
            Bitmap BinarizedImage = new Bitmap(width, height);

            int T = GetOSTUThreshold(Lights2);

            progressBar1.Value = 0;
            progressBar1.Maximum = SourceBitmap.Height;
            progressBar1.Step = 1;

            for (int h = 0; h < SourceBitmap.Height; h++)
            {
                for (int w = 0; w < SourceBitmap.Width; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        if (SourceColors[h][w] < T) BinarizedImage.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                        else BinarizedImage.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else BinarizedImage.SetPixel(w, h, SourceBitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }

            return BinarizedImage;
        }

        public static int GetOSTUThreshold(int[] HistGram)
        {
            int Y, Amount = 0;
            int PixelBack = 0, PixelFore = 0, PixelIntegralBack = 0, PixelIntegralFore = 0, PixelIntegral = 0;
            double OmegaBack, OmegaFore, MicroBack, MicroFore, SigmaB, Sigma;
            int MinValue, MaxValue;
            int Threshold = 0;

            
            for (MinValue = 0; MinValue < 1024 && HistGram[MinValue] == 0; MinValue++) ;
            for (MaxValue = 1023; MaxValue > MinValue && HistGram[MinValue] == 0; MaxValue--) ;
            if (MaxValue == MinValue)
                return MaxValue;             
            if (MinValue + 1 == MaxValue)
                return MinValue; 

            for (Y = MinValue; Y <= MaxValue; Y++)
            {
                Amount += HistGram[Y];
            }

            PixelIntegral = 0;
            for (Y = MinValue; Y <= MaxValue; Y++)
            {
                PixelIntegral += HistGram[Y] * Y;
            }
            SigmaB = -1;
            for (Y = MinValue; Y < MaxValue; Y++)
            {
                PixelBack = PixelBack + HistGram[Y]; 
                PixelFore = Amount - PixelBack; 
                OmegaBack = (double)PixelBack / Amount;
                OmegaFore = (double)PixelFore / Amount; 
                PixelIntegralBack += HistGram[Y] * Y; 
                PixelIntegralFore = PixelIntegral - PixelIntegralBack;
                MicroBack = (double)PixelIntegralBack / PixelBack;
                MicroFore = (double)PixelIntegralFore / PixelFore;
                Sigma = OmegaBack * OmegaFore * (MicroBack - MicroFore) * (MicroBack - MicroFore);
                if (Sigma > SigmaB)
                {
                    SigmaB = Sigma;
                    Threshold = Y;
                }
            }
            return Threshold;
        }

        private void DilatationClick(object sender, EventArgs e)
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            Bitmap Tmpbitmap = new Bitmap(pictureBox1.Image);
            pictureBox1.Image = Dilatation(Tmpbitmap);
        }

        private Bitmap Dilatation(Bitmap SourceBitmap)
        {
            Bitmap DilatatedBitmap = new Bitmap(SourceBitmap.Width, SourceBitmap.Height);
            int offset = 2;

            progressBar1.Value = 0;
            progressBar1.Maximum = height - offset - offset;
            progressBar1.Step = 1;

            for (int h = offset; h < height - offset; h++)
            {
                for (int w = offset; w < width - offset; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        if (ForDilatation(w, h, offset, SourceBitmap)) DilatatedBitmap.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                        else DilatatedBitmap.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else DilatatedBitmap.SetPixel(w, h, SourceBitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }
            return DilatatedBitmap;
        }

        private Boolean ForDilatation(int w, int h, int offset, Bitmap Sourcebitmap)
        {
            int sum = 0;
            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    sum += S_Element[i - (h - offset), j - (w - offset)] * Sourcebitmap.GetPixel(j, i).R;
                }
            }
            if (sum == 0) return true;
            else return false;
        }

        private void ErosionClick(object sender, EventArgs e)
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            Bitmap Tmpbitmap = new Bitmap(pictureBox1.Image);
            pictureBox1.Image = Erosion(Tmpbitmap);
        }

        private Bitmap Erosion(Bitmap SourceBitmap)
        {
            Bitmap ErosionBitmap = new Bitmap(SourceBitmap.Width, SourceBitmap.Height);
            int offset = 2;

            progressBar1.Value = 0;
            progressBar1.Maximum = height - offset - offset;
            progressBar1.Step = 1;

            for (int h = offset; h < height - offset; h++)
            {
                for (int w = offset; w < width - offset; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    {
                        if (SourceBitmap.GetPixel(w, h).R == 0) ForErosion(w, h, offset, ErosionBitmap);
                        else ErosionBitmap.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else ErosionBitmap.SetPixel(w, h, SourceBitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }
            return ErosionBitmap;
        }

        private void ForErosion(int w, int h, int offset, Bitmap Resultbitmap)
        {
            for (int i = h - offset; i <= h + offset; i++)
            {
                for (int j = w - offset; j <= w + offset; j++)
                {
                    if (S_Element[i - (h - offset), j - (w - offset)] == 1)
                    Resultbitmap.SetPixel(j, i, Color.FromArgb(0, 0, 0));
                }
            }
        }

        private void SetSaveTopBorder(object sender, EventArgs e)//задание верхней границы сохранения
        {
            int flag;
            if (textBox3.Text.Length == 0)
            {
                textBox3.Text = "0";
            }
            else if (Int32.TryParse(textBox3.Text, out flag) == false)//если пользователь попробует ввести нечисловое значение
            {
                textBox3.Text = "0";
                MessageBox.Show($"введите числовое значение Верхней границы");
            }
            else SaveTopBorder = Int32.Parse(textBox3.Text);
        }

        private void SetSaveBotBorder(object sender, EventArgs e)//задание нижней границы сохранения
        {
            int flag;
            if (textBox4.Text.Length == 0)
            {
                textBox4.Text = "0";
            }
            else if (Int32.TryParse(textBox4.Text, out flag) == false)//если пользователь попробует ввести нечисловое значение
            {
                textBox4.Text = "0";
                MessageBox.Show($"введите числовое значение Нижней границы");
            }
            else SaveBotBorder = Int32.Parse(textBox4.Text);
        }        

        private void button11_Click(object sender, EventArgs e)//нажатие на кнопку "По Отцу"
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = CvartOtsu(NormalizedBitmap, NormalizedColors);
            BinRegularBmap = CvartOtsu(bitmap, Colors);
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 6;

            IsBinarized = true;
        }

        private Bitmap CvartOtsu(Bitmap SourceBitmap, short[][] SourceColors)//Квартеризация на основе бинаризации Отцу
        {
            Bitmap BinarizedImage = new Bitmap(width, height);

            int T = GetOSTUThreshold(Lights2);
            
            int[] ForLeftT = new int[1024];
            int[] ForRightT = new int[1024];
            for (int i = 0; i < 1024; i++)
            {
                if (i < T)
                {
                    ForLeftT[i] = Lights2[i];
                    ForRightT[i] = 0;
                }
                else
                {
                    ForLeftT[i] = 0;
                    ForRightT[i] = Lights2[i];
                }

            }

            int LeftT = GetOSTUThreshold(ForLeftT);
            int RightT = GetOSTUThreshold(ForRightT);


            progressBar1.Value = 0;
            progressBar1.Maximum = SourceBitmap.Height;
            progressBar1.Step = 1;

            for (int h = 0; h < SourceBitmap.Height; h++)
            {
                for (int w = 0; w < SourceBitmap.Width; w++)
                {
                    if (h <= BotBorder && h >= TopBorder)
                    { 
                        int TempColor = SourceColors[h][w];
                        if (TempColor < LeftT) BinarizedImage.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                        else if (TempColor >= LeftT && TempColor < T) BinarizedImage.SetPixel(w, h, Color.FromArgb(64, 64, 64));
                        else if (TempColor >= T && TempColor < RightT) BinarizedImage.SetPixel(w, h, Color.FromArgb(128, 128, 128));
                        else BinarizedImage.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                    }
                    else BinarizedImage.SetPixel(w, h, SourceBitmap.GetPixel(w, h));
                }
                progressBar1.PerformStep();
            }

            return BinarizedImage;
        }

        private void button12_Click(object sender, EventArgs e)//нажатие на кнопку "По Брэдли2"
        {
            ReservedBitmap = new Bitmap(pictureBox1.Image);

            BinNormalizedBmap = CvartBradley(NormalizedBitmap, NormalizedColors);
            BinRegularBmap = CvartBradley(bitmap, Colors);
            if (checkBox1.Checked)
            {
                pictureBox1.Image = BinNormalizedBmap;
            }
            else
            {
                pictureBox1.Image = BinRegularBmap;
            }
            view = 7;

            IsBinarized = true;
        }

        private Bitmap CvartBradley(Bitmap Sourcebitmap, short[][] SourceColors)//Квартеризация на основе (Метод Брэдли2)
        {
            int d = width / 8;
            int Wcount = width / d;
            int Hcount = height / d;
            int[][] T;
            int count = d * d;
            int sum = 0;
            Bitmap BinarizedImage = new Bitmap(width, height);

            progressBar1.Value = 0;
            progressBar1.Maximum = Hcount;
            progressBar1.Step = 1;

            T = new int[Hcount][];//заполнение массива

            int[][] LeftT, RightT;//для квартеризации
            LeftT = new int[Hcount][];
            RightT = new int[Hcount][];
            int LeftSum = 0, RightSum = 0, LeftCount = 0, RightCount = 0;

            for (int i = 0; i < Hcount; i++)
            {
                T[i] = new int[Wcount];

                LeftT[i] = new int[Wcount];//для квартеризации
                RightT[i] = new int[Wcount];
            }

            for (int i = 0; i < Hcount; i++)
            {
                for (int j = 0; j < Wcount; j++)
                {
                    for (int h = 0; h < d; h++)
                    {
                        for (int w = 0; w < d; w++)
                        {
                            sum += SourceColors[h + d * i][w + d * j];
                        }
                    }
                    int avg = sum / count;
                    int t = (int)(avg * 0.15);
                    T[i][j] = avg + t;
                    sum = 0;
                }
            }

            for (int i = 0; i < Hcount; i++)//для квартеризации
            {
                for (int j = 0; j < Wcount; j++)
                {
                    for (int h = 0; h < d; h++)
                    {
                        for (int w = 0; w < d; w++)
                        {
                            int temp = SourceColors[h + d * i][w + d * j];
                            if (temp < T[i][j])
                            {
                                LeftSum += temp;
                                LeftCount++;
                            }
                            else
                            {
                                RightSum += temp;
                                RightCount++;
                            }
                        }
                    }
                    if (LeftSum != 0) 
                    {
                        int LeftAvg = LeftSum / LeftCount;
                        int Leftt = (int)(LeftAvg * 0.15);
                        LeftT[i][j] = LeftAvg + Leftt;
                        LeftSum = 0;
                        LeftCount = 0;
                    }
                    if (RightSum != 0) 
                    {
                        int RightAvg = RightSum / RightCount;
                        int Rightt = (int)(RightAvg * 0.15);
                        RightT[i][j] = RightAvg + Rightt;
                        RightSum = 0;
                        RightCount = 0;
                    }                    
                }
            }

            for (int i = 0; i < Hcount; i++)
            {
                for (int j = 0; j < Wcount; j++)
                {
                    for (int h = 0; h < d; h++)
                    {
                        for (int w = 0; w < d; w++)
                        {
                            int x = w + d * j;
                            int y = h + d * i;

                            if (y <= BotBorder && y >= TopBorder)
                            {
                                int temp = SourceColors[y][x];//для квартеризации
                                if (temp < LeftT[i][j]) BinarizedImage.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                                else if (temp >= LeftT[i][j] && temp < T[i][j]) BinarizedImage.SetPixel(x, y, Color.FromArgb(64, 64, 64));
                                else if (temp >= T[i][j] && temp < RightT[i][j]) BinarizedImage.SetPixel(x, y, Color.FromArgb(128, 128, 128));
                                else BinarizedImage.SetPixel(x, y, Color.FromArgb(255, 255, 255));
                            }
                            else BinarizedImage.SetPixel(x, y, Sourcebitmap.GetPixel(x, y));
                        }
                    }
                }
                progressBar1.PerformStep();
            }

            return BinarizedImage;
        }


        private void button13_Click(object sender, EventArgs e)//Кнопка "Шаг назад"
        {
            if (ReservedBitmap != null) pictureBox1.Image = ReservedBitmap;
        }

        private void button14_Click(object sender, EventArgs e)//выбор структурирующего элемента (0,0)
        {
            if (button14.BackColor == Color.White)
            {
                button14.BackColor = Color.Black;
                S_Element[0,0] = 1;
            }
            else
            {
                button14.BackColor = Color.White;
                S_Element[0, 0] = 0;
            }
        }

        private void button15_Click(object sender, EventArgs e)//выбор структурирующего элемента (0,1)
        {
            if (button15.BackColor == Color.White)
            {
                button15.BackColor = Color.Black;
                S_Element[0, 1] = 1;
            }
            else
            {
                button15.BackColor = Color.White;
                S_Element[0, 1] = 0;
            }
        }

        private void button16_Click(object sender, EventArgs e)//выбор структурирующего элемента (0,2)
        {
            if (button16.BackColor == Color.White)
            {
                button16.BackColor = Color.Black;
                S_Element[0, 2] = 1;
            }
            else
            {
                button16.BackColor = Color.White;
                S_Element[0, 2] = 0;
            }
        }

        private void button23_Click(object sender, EventArgs e)//выбор структурирующего элемента (0,3)
        {
            if (button23.BackColor == Color.White)
            {
                button23.BackColor = Color.Black;
                S_Element[0, 3] = 1;
            }
            else
            {
                button23.BackColor = Color.White;
                S_Element[0, 3] = 0;
            }
        }

        private void button24_Click(object sender, EventArgs e)//выбор структурирующего элемента (0,4)
        {
            if (button24.BackColor == Color.White)
            {
                button24.BackColor = Color.Black;
                S_Element[0, 4] = 1;
            }
            else
            {
                button24.BackColor = Color.White;
                S_Element[0, 4] = 0;
            }
        }

        private void button17_Click(object sender, EventArgs e)//выбор структурирующего элемента (1,0)
        {
            if (button17.BackColor == Color.White)
            {
                button17.BackColor = Color.Black;
                S_Element[1, 0] = 1;
            }
            else
            {
                button17.BackColor = Color.White;
                S_Element[1, 0] = 0;
            }
        }

        private void button18_Click(object sender, EventArgs e)//выбор структурирующего элемента (1,1)
        {
            if (button18.BackColor == Color.White)
            {
                button18.BackColor = Color.Black;
                S_Element[1, 1] = 1;
            }
            else
            {
                button18.BackColor = Color.White;
                S_Element[1, 1] = 0;
            }
        }

        private void button19_Click(object sender, EventArgs e)//выбор структурирующего элемента (1,2)
        {
            if (button19.BackColor == Color.White)
            {
                button19.BackColor = Color.Black;
                S_Element[1, 2] = 1;
            }
            else
            {
                button19.BackColor = Color.White;
                S_Element[1, 2] = 0;
            }
        }

        private void button25_Click(object sender, EventArgs e)//выбор структурирующего элемента (1,3)
        {
            if (button25.BackColor == Color.White)
            {
                button25.BackColor = Color.Black;
                S_Element[1, 3] = 1;
            }
            else
            {
                button25.BackColor = Color.White;
                S_Element[1, 3] = 0;
            }
        }

        private void button26_Click(object sender, EventArgs e)//выбор структурирующего элемента (1,4)
        {
            if (button26.BackColor == Color.White)
            {
                button26.BackColor = Color.Black;
                S_Element[1, 4] = 1;
            }
            else
            {
                button26.BackColor = Color.White;
                S_Element[1, 4] = 0;
            }
        }

        private void button20_Click(object sender, EventArgs e)//выбор структурирующего элемента (2,0)
        {
            if (button20.BackColor == Color.White)
            {
                button20.BackColor = Color.Black;
                S_Element[2, 0] = 1;
            }
            else
            {
                button20.BackColor = Color.White;
                S_Element[2, 0] = 0;
            }
        }

        private void button21_Click(object sender, EventArgs e)//выбор структурирующего элемента (2,1)
        {
            if (button21.BackColor == Color.White)
            {
                button21.BackColor = Color.Black;
                S_Element[2, 1] = 1;
            }
            else
            {
                button21.BackColor = Color.White;
                S_Element[2, 1] = 0;
            }
        }

        private void button22_Click(object sender, EventArgs e)//выбор структурирующего элемента (2,2)
        {
            if (button22.BackColor == Color.White)
            {
                button22.BackColor = Color.Black;
                S_Element[2, 2] = 1;
            }
            else
            {
                button22.BackColor = Color.White;
                S_Element[2, 2] = 0;
            }
        }

        private void button27_Click(object sender, EventArgs e)//выбор структурирующего элемента (2,3)
        {
            if (button27.BackColor == Color.White)
            {
                button27.BackColor = Color.Black;
                S_Element[2, 3] = 1;
            }
            else
            {
                button27.BackColor = Color.White;
                S_Element[2, 3] = 0;
            }
        }

        private void button28_Click(object sender, EventArgs e)//выбор структурирующего элемента (2,4)
        {
            if (button28.BackColor == Color.White)
            {
                button28.BackColor = Color.Black;
                S_Element[2, 4] = 1;
            }
            else
            {
                button28.BackColor = Color.White;
                S_Element[2, 4] = 0;
            }
        }

        private void button29_Click(object sender, EventArgs e)//выбор структурирующего элемента (3,0)
        {
            if (button29.BackColor == Color.White)
            {
                button29.BackColor = Color.Black;
                S_Element[3, 0] = 1;
            }
            else
            {
                button29.BackColor = Color.White;
                S_Element[3, 0] = 0;
            }
        }

        private void button30_Click(object sender, EventArgs e)//выбор структурирующего элемента (3,1)
        {
            if (button30.BackColor == Color.White)
            {
                button30.BackColor = Color.Black;
                S_Element[3, 1] = 1;
            }
            else
            {
                button30.BackColor = Color.White;
                S_Element[3, 1] = 0;
            }
        }

        private void button31_Click(object sender, EventArgs e)//выбор структурирующего элемента (3,2)
        {
            if (button31.BackColor == Color.White)
            {
                button31.BackColor = Color.Black;
                S_Element[3, 2] = 1;
            }
            else
            {
                button31.BackColor = Color.White;
                S_Element[3, 2] = 0;
            }
        }

        private void button32_Click(object sender, EventArgs e)//выбор структурирующего элемента (3,3)
        {
            if (button32.BackColor == Color.White)
            {
                button32.BackColor = Color.Black;
                S_Element[3, 3] = 1;
            }
            else
            {
                button32.BackColor = Color.White;
                S_Element[3, 3] = 0;
            }
        }

        private void button33_Click(object sender, EventArgs e)//выбор структурирующего элемента (3,4)
        {
            if (button33.BackColor == Color.White)
            {
                button33.BackColor = Color.Black;
                S_Element[3, 4] = 1;
            }
            else
            {
                button33.BackColor = Color.White;
                S_Element[3, 4] = 0;
            }
        }

        private void button34_Click(object sender, EventArgs e)//выбор структурирующего элемента (4,0)
        {
            if (button34.BackColor == Color.White)
            {
                button34.BackColor = Color.Black;
                S_Element[4, 0] = 1;
            }
            else
            {
                button34.BackColor = Color.White;
                S_Element[4, 0] = 0;
            }
        }

        private void button35_Click(object sender, EventArgs e)//выбор структурирующего элемента (4,1)
        {
            if (button35.BackColor == Color.White)
            {
                button35.BackColor = Color.Black;
                S_Element[4, 1] = 1;
            }
            else
            {
                button35.BackColor = Color.White;
                S_Element[4, 1] = 0;
            }
        }

        private void button36_Click(object sender, EventArgs e)//выбор структурирующего элемента (4,2)
        {
            if (button36.BackColor == Color.White)
            {
                button36.BackColor = Color.Black;
                S_Element[4, 2] = 1;
            }
            else
            {
                button36.BackColor = Color.White;
                S_Element[4, 2] = 0;
            }
        }

        private void button37_Click(object sender, EventArgs e)//выбор структурирующего элемента (4,3)
        {
            if (button37.BackColor == Color.White)
            {
                button37.BackColor = Color.Black;
                S_Element[4, 3] = 1;
            }
            else
            {
                button37.BackColor = Color.White;
                S_Element[4, 3] = 0;
            }
        }

        private void button38_Click(object sender, EventArgs e)//выбор структурирующего элемента (4,4)
        {
            if (button38.BackColor == Color.White)
            {
                button38.BackColor = Color.Black;
                S_Element[4, 4] = 1;
            }
            else
            {
                button38.BackColor = Color.White;
                S_Element[4, 4] = 0;
            }
        }
        
        private void button40_Click(object sender, EventArgs e)//Совмещение
        {
            Form2 f = new Form2();
            f.Show();
        }

        //функция нормализации
        private short[][] Normalization(short[][] Arr, int height, int width, int NEWMAX, int NEWMIN)
        {
            double max = Arr[0][0];
            double min = Arr[0][0];
            double newmax = NEWMAX;
            double newmin = NEWMIN;

            short[][] NormalizedArr = new short[height][];//заполнение массива цветов
            for (int i = 0; i < height; i++)
            {
                NormalizedArr[i] = new short[width];
            }

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (Arr[h][w] < min)
                        min = Arr[h][w];
                    if (Arr[h][w] > max)
                        max = Arr[h][w];
                }
            }
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    NormalizedArr[h][w] = (short)(((Arr[h][w] - min) * ((newmax - newmin) / (max - min))) + newmin);//формула нормализации
                }
            }
            return NormalizedArr;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)//Нормализовать яркости
        {
            if (File != null)
            {
                switch (view)
                {
                    case 0://загруженное изображение и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = NormalizedBitmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = OffsetBitmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 1://бинаризация Бернсена и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 2://бинаризация Брэдли и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 3://бинаризация Брэдли2 и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 4://бинаризация Ниблэка и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 5://бинаризация Отцу и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 6://квартеризация по Отцу и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;

                    case 7://квартеризация по Брэдли2 и его нормализированная версия
                        if (checkBox1.Checked)
                        {
                            pictureBox1.Image = BinNormalizedBmap;
                            pictureBox4.Image = NormalizedHystBitmap;
                        }
                        else
                        {
                            pictureBox1.Image = BinRegularBmap;
                            pictureBox4.Image = HystBitmap;
                        }
                        break;
                }
            }            
        }

        private void button39_Click(object sender, EventArgs e)//сохранить изображение в формате PNG
        {
            //saveFileDialog1.Filter = "PNG|*.png";
            saveFileDialog1.FileName = txtFileName.Text;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    String FilePath = saveFileDialog1.FileName;//задание пути к файлу
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Save(FilePath);
                    }
                    else MessageBox.Show("Ошибка");
                }
                catch
                {
                    MessageBox.Show("Ошибка");
                }
            }
        }

        private void button41_Click(object sender, EventArgs e)//градиент изображения
        {
            pictureBox1.Image = Grad(bitmap);
        }

        private Bitmap Grad(Bitmap Source)
        {
            //Bitmap GradImage = new Bitmap(width, height);
            Bitmap GradImage = new Bitmap(pictureBox1.Image);
            double grad;

            for (int h = 0; h < height; h++)
            {
                grad = 1;
                for (int w = 0; w < width; w++)
                {
                    //short temp = (short)(Source.GetPixel(w, h).R * grad);
                    short temp = (short)(GradImage.GetPixel(w, h).R * grad);

                    if (temp < 256)//обработка исключения и задание пикселям цветов
                    {
                        GradImage.SetPixel(w, h, Color.FromArgb(temp, temp, temp));
                    }
                    else
                    {                        
                        GradImage.SetPixel(w, h, Color.FromArgb(temp % 256, temp % 256, temp % 256));
                    }

                    grad -= 0.0005;
                    temp = 0;
                }
            }
            return GradImage;
        }

        private void button42_Click(object sender, EventArgs e)
        {
            //saveFileDialog1.Filter = "PNG|*.png";
            saveFileDialog1.FileName = txtFileName.Text;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    String FilePath = saveFileDialog1.FileName;//задание пути к файлу
                    if (pictureBox4.Image != null)
                    {
                        pictureBox4.Image.Save(FilePath);
                    }
                    else MessageBox.Show("Ошибка");
                }
                catch
                {
                    MessageBox.Show("Ошибка");
                }
            }
        }

    }
}