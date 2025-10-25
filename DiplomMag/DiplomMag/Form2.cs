using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DiplomMag
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        int upString = 0;//верхняя строка
        short offset = 0;// сдвиг
        int mode = 0;//режим совмещения

        private void button1_Click(object sender, EventArgs e)//Загрузить1
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String FilePath = openFileDialog1.FileName;//задание пути к файлу
                String Filename = openFileDialog1.SafeFileName;//задание имени файла
                byte[] File = System.IO.File.ReadAllBytes(FilePath);

                byte[] widthBytes = new byte[4], heightBytes = new byte[4], ColorBytes = new byte[2];//ширина, высота и яркость пикселя

                Array.Copy(File, widthBytes, 4);
                Array.Copy(File, 4, heightBytes, 0, 4);
                int width = BitConverter.ToInt32(widthBytes, 0);
                int height = BitConverter.ToInt32(heightBytes, 0);

                if (width > 6104 | height > 100000)
                {
                    MessageBox.Show($"изображение слишком большое");
                    return;
                }
                short[][] Colors = new short[height][];//заполнение массива цветов
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

                short FreeColors;//переменная, используящаяся для реализации сдвига кода

                Bitmap OffsetBitmap = new Bitmap(width, height);
                short OffsetColors;

                progressBar1.Value = 0;
                progressBar1.Maximum = height;
                progressBar1.Step = 1;

                for (int h = upString; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        FreeColors = Colors[h][w];
                        OffsetColors = Colors[h][w];
                        OffsetColors >>= offset;
                        if (FreeColors < 256)//обработка исключения и задание пикселям цветов с учетом верхних строк
                        {
                            OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors, OffsetColors, OffsetColors));
                        }
                        else
                        {
                            OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors % 256, OffsetColors % 256, OffsetColors % 256));
                        }
                    }
                    progressBar1.PerformStep();
                }
                pictureBox1.Height = height - upString;//задание высоты контейнера для изображения
                pictureBox1.Width = width;//задание ширины контейнера для изображения
                pictureBox1.Image = OffsetBitmap;
                
                txtFileName1.Text = Filename;
                txtImageSize1.Text = width.ToString() + " X " + height.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)//Загрузить2
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String FilePath = openFileDialog1.FileName;//задание пути к файлу
                String Filename = openFileDialog1.SafeFileName;//задание имени файла
                byte[] File = System.IO.File.ReadAllBytes(FilePath);

                byte[] widthBytes = new byte[4], heightBytes = new byte[4], ColorBytes = new byte[2];//ширина, высота и яркость пикселя

                Array.Copy(File, widthBytes, 4);
                Array.Copy(File, 4, heightBytes, 0, 4);
                int width = BitConverter.ToInt32(widthBytes, 0);
                int height = BitConverter.ToInt32(heightBytes, 0);

                if (width > 6104 | height > 100000)
                {
                    MessageBox.Show($"изображение слишком большое");
                    return;
                }
                short[][] Colors = new short[height][];//заполнение массива цветов
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
                short FreeColors;//переменная, используящаяся для реализации сдвига кода

                Bitmap OffsetBitmap = new Bitmap(width, height);
                short OffsetColors;

                progressBar1.Value = 0;
                progressBar1.Maximum = height;
                progressBar1.Step = 1;

                for (int h = upString; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        FreeColors = Colors[h][w];
                        OffsetColors = Colors[h][w];
                        OffsetColors >>= offset;
                        if (FreeColors < 256)//обработка исключения и задание пикселям цветов с учетом верхних строк
                        {
                            OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors, OffsetColors, OffsetColors));
                        }
                        else
                        {
                            OffsetBitmap.SetPixel(w, h - upString, Color.FromArgb(OffsetColors % 256, OffsetColors % 256, OffsetColors % 256));
                        }
                    }
                    progressBar1.PerformStep();
                }
                pictureBox2.Height = height - upString;//задание высоты контейнера для изображения
                pictureBox2.Width = width;//задание ширины контейнера для изображения
                pictureBox2.Image = OffsetBitmap;
                txtFileName2.Text = Filename;
                txtImageSize2.Text = width.ToString() + " X " + height.ToString();
            }
        }

        private void Coordinates1(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                Xcoordinate1.Text = e.X.ToString();
                Ycoordinate1.Text = e.Y.ToString();
            }
        }

        private void Coordinates2(object sender, MouseEventArgs e)
        {
            if (pictureBox2.Image != null)
            {
                Xcoordinate2.Text = e.X.ToString();
                Ycoordinate2.Text = e.Y.ToString();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                Xcombining1.Text = Xcoordinate1.Text;
                Ycombining1.Text = Ycoordinate1.Text;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image != null)
            {
                Xcombining2.Text = Xcoordinate2.Text;
                Ycombining2.Text = Ycoordinate2.Text;
            }
        }

        private void button3_Click(object sender, EventArgs e)//кнопка "Совместить"
        {
            Bitmap First = new Bitmap(pictureBox1.Image);
            Bitmap Second = new Bitmap(pictureBox2.Image);

            int Xdif = Int32.Parse(Xcombining2.Text) - Int32.Parse(Xcombining1.Text);
            int Ydif = Int32.Parse(Ycombining2.Text) - Int32.Parse(Ycombining1.Text);
            int X = Math.Abs(Xdif);
            int Y = Math.Abs(Ydif);

            Bitmap Combined = new Bitmap(First.Width - X, First.Height - Y);
            pictureBox3.Height = First.Height - Y;
            pictureBox3.Width = First.Width - X;

            progressBar1.Value = 0;
            progressBar1.Maximum = First.Height - Y;
            progressBar1.Step = 1;

            
            for (int h = 0; h < First.Height - Y; h++)
            {
                for (int w = 0; w < First.Width - X; w++)
                {
                    if (Xdif <= 0 && Ydif >= 0)//1 случай
                    {
                        switch (mode)
                        {
                            case 0://Спектрозональный режим (красный спектр из первой картинки, зеленый из второй)
                                Combined.SetPixel(w, h, Color.FromArgb(First.GetPixel(w + X, h).R, Second.GetPixel(w, h + Y).G, 0));
                                break;

                            case 1://Режим для вычисления % несовпадений при совмещении
                                if (First.GetPixel(w + X, h).R == Second.GetPixel(w, h + Y).G) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;

                            case 2://Режим для квартеризированных изображений
                                short firstpix = First.GetPixel(w + X, h).R;
                                short secondpix = Second.GetPixel(w, h + Y).G;
                                if (firstpix == secondpix) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else if (firstpix == 0 && secondpix == 64 || firstpix == 64 && secondpix == 0 || firstpix == 64 && secondpix == 128 || firstpix == 128 && secondpix == 64 || firstpix == 128 && secondpix == 255 || firstpix == 255 && secondpix == 128)
                                {
                                    Combined.SetPixel(w, h, Color.FromArgb(64, 64, 64));
                                }
                                else if (firstpix == 0 && secondpix == 128 || firstpix == 64 && secondpix == 255 || firstpix == 128 && secondpix == 0 || firstpix == 255 && secondpix == 64) Combined.SetPixel(w, h, Color.FromArgb(128, 128, 128));
                                else if (firstpix == 0 && secondpix == 255 || firstpix == 255 && secondpix == 0) Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;
                        }
                    }
                    else if (Xdif >= 0 && Ydif >= 0)//2 случай
                    {
                        switch (mode)
                        {
                            case 0://Спектрозональный режим (красный спектр из первой картинки, зеленый из второй)
                                Combined.SetPixel(w, h, Color.FromArgb(First.GetPixel(w, h).R, Second.GetPixel(w + X, h + Y).G, 0));
                                break;

                            case 1://Режим для вычисления % несовпадений при совмещении
                                if (First.GetPixel(w, h).R == Second.GetPixel(w + X, h + Y).G) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;

                            case 2://Режим для квартеризированных изображений
                                short firstpix = First.GetPixel(w, h).R;
                                short secondpix = Second.GetPixel(w + X, h + Y).G;
                                if (firstpix == secondpix) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else if (firstpix == 0 && secondpix == 64 || firstpix == 64 && secondpix == 0 || firstpix == 64 && secondpix == 128 || firstpix == 128 && secondpix == 64 || firstpix == 128 && secondpix == 255 || firstpix == 255 && secondpix == 128)
                                {
                                    Combined.SetPixel(w, h, Color.FromArgb(64, 64, 64));
                                }
                                else if (firstpix == 0 && secondpix == 128 || firstpix == 64 && secondpix == 255 || firstpix == 128 && secondpix == 0 || firstpix == 255 && secondpix == 64) Combined.SetPixel(w, h, Color.FromArgb(128, 128, 128));
                                else if (firstpix == 0 && secondpix == 255 || firstpix == 255 && secondpix == 0) Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;
                        }
                    }
                    else if (Xdif <= 0 && Ydif <= 0)//3 случай
                    {
                        switch (mode)
                        {
                            case 0://Спектрозональный режим (красный спектр из первой картинки, зеленый из второй)
                                Combined.SetPixel(w, h, Color.FromArgb(First.GetPixel(w + X, h + Y).R, Second.GetPixel(w, h).G, 0));
                                break;

                            case 1://Режим для вычисления % несовпадений при совмещении
                                if (First.GetPixel(w + X, h + Y).R == Second.GetPixel(w, h).G) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;

                            case 2://Режим для квартеризированных изображений
                                short firstpix = First.GetPixel(w + X, h + Y).R;
                                short secondpix = Second.GetPixel(w, h).G;
                                if (firstpix == secondpix) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else if (firstpix == 0 && secondpix == 64 || firstpix == 64 && secondpix == 0 || firstpix == 64 && secondpix == 128 || firstpix == 128 && secondpix == 64 || firstpix == 128 && secondpix == 255 || firstpix == 255 && secondpix == 128)
                                {
                                    Combined.SetPixel(w, h, Color.FromArgb(64, 64, 64));
                                }
                                else if (firstpix == 0 && secondpix == 128 || firstpix == 64 && secondpix == 255 || firstpix == 128 && secondpix == 0 || firstpix == 255 && secondpix == 64) Combined.SetPixel(w, h, Color.FromArgb(128, 128, 128));
                                else if (firstpix == 0 && secondpix == 255 || firstpix == 255 && secondpix == 0) Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;
                        }
                    }
                    else if (Xdif >= 0 && Ydif <= 0)//4 случай
                    {
                        switch (mode)
                        {
                            case 0://Спектрозональный режим (красный спектр из первой картинки, зеленый из второй)
                                Combined.SetPixel(w, h, Color.FromArgb(First.GetPixel(w, h + Y).R, Second.GetPixel(w + X, h).G, 0));
                                break;

                            case 1://Режим для вычисления % несовпадений при совмещении
                                if (First.GetPixel(w, h + Y).R == Second.GetPixel(w + X, h).G) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;

                            case 2://Режим для квартеризированных изображений
                                short firstpix = First.GetPixel(w, h + Y).R;
                                short secondpix = Second.GetPixel(w + X, h).G;
                                if (firstpix == secondpix) Combined.SetPixel(w, h, Color.FromArgb(0, 0, 0));
                                else if (firstpix == 0 && secondpix == 64 || firstpix == 64 && secondpix == 0 || firstpix == 64 && secondpix == 128 || firstpix == 128 && secondpix == 64 || firstpix == 128 && secondpix == 255 || firstpix == 255 && secondpix == 128)
                                {
                                    Combined.SetPixel(w, h, Color.FromArgb(64, 64, 64));
                                }
                                else if (firstpix == 0 && secondpix == 128 || firstpix == 64 && secondpix == 255 || firstpix == 128 && secondpix == 0 || firstpix == 255 && secondpix == 64) Combined.SetPixel(w, h, Color.FromArgb(128, 128, 128));
                                else if (firstpix == 0 && secondpix == 255 || firstpix == 255 && secondpix == 0) Combined.SetPixel(w, h, Color.FromArgb(255, 255, 255));
                                break;
                        }
                    }
                }
                progressBar1.PerformStep();
            }

            pictureBox3.Image = Combined;
        }

        private void radioButton1_Click(object sender, EventArgs e)
        {
            mode = 0;
        }

        private void radioButton2_Click(object sender, EventArgs e)
        {
            mode = 1;
        }

        private void radioButton3_Click(object sender, EventArgs e)
        {
            mode = 2;
        }

        private void button4_Click(object sender, EventArgs e)//Информативность
        {            
            Form3 f = new Form3(pictureBox1.Image, pictureBox2.Image, pictureBox3.Image, mode);
            f.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //saveFileDialog1.Filter = "PNG|*.png";
            saveFileDialog1.FileName = txtFileName1.Text;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    String FilePath = saveFileDialog1.FileName;//задание пути к файлу
                    if (pictureBox3.Image != null)
                    {
                        pictureBox3.Image.Save(FilePath);
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
