using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace DiplomMag
{
    public partial class Form3 : Form
    {
        Image First;
        Image Second;
        Image Combined;
        int mode;

        public Form3(Image pic1, Image pic2, Image pic3, int mode)
        {
            InitializeComponent();

            First = pic1;
            Second = pic2;
            Combined = pic3;
            this.mode = mode;

            comboBox1.SelectedIndex = 2;
        }

        private int[,] Informationfuncktion(Image img, PictureBox pic, DataGridView datagrid, int size, Panel panel, int mode = 0, DataGridView datagrid2 = null)
        {
            int width = (img.Width - img.Width % size);
            int height = (img.Height - img.Height % size);
            int WidthSquare = width / size;
            int HeightSquare = height / size;
            Bitmap bitmap = new Bitmap(img);
            int InformationSquare = 0;
            int[,] InformationSquareArr = new int[HeightSquare, WidthSquare];
            int[,] NormalizedInformationSquareArr;
            Bitmap Picbitmap = new Bitmap(WidthSquare * 10, HeightSquare * 10);


            int temp = size * size;
            if (mode == 1 && img == Combined)
            {
                int[,] UncombinedSquareArr = new int[HeightSquare, WidthSquare];
                int WhiteCount = 0;


                for (int h = 0; h < HeightSquare; h++)
                {
                    for (int w = 0; w < WidthSquare; w++)
                    {
                        for (int i = h * size; i < h * size + size - 1; i++)
                        {
                            for (int j = w * size; j < w * size + size - 1; j++)
                            {
                                int temppix = bitmap.GetPixel(j, i).R;
                                InformationSquare += Math.Abs(bitmap.GetPixel(j, i + 1).R - temppix) + Math.Abs(bitmap.GetPixel(j + 1, i).R - temppix);
                                if (temppix == 255) WhiteCount += 1;                       
                            }
                        }
                        InformationSquareArr[h, w] = InformationSquare / temp;
                        InformationSquare = 0;

                        UncombinedSquareArr[h, w] = WhiteCount * 100 / temp;
                        WhiteCount = 0;
                    }
                }

                datagrid2.Rows.Clear();
                datagrid2.Columns.Clear();
                for (int w = 0; w < WidthSquare; w++)
                {
                    datagrid2.Columns.Add("name", null);
                }
                for (int h = 0; h < HeightSquare; h++)
                {
                    datagrid2.Rows.Add();
                }
                for (int h = 0; h < HeightSquare; h++)
                {
                    for (int w = 0; w < WidthSquare; w++)
                    {
                        datagrid2.Rows[h].Cells[w].Value = UncombinedSquareArr[h, w];
                    }
                }

                pictureBox7.Image = CreateGraphic(InformationSquareArr, UncombinedSquareArr, pictureBox7);
            }
            else 
            {
                for (int h = 0; h < HeightSquare; h++)
                {
                    for (int w = 0; w < WidthSquare; w++)
                    {
                        for (int i = h * size; i < h * size + size - 1; i++)
                        {
                            for (int j = w * size; j < w * size + size - 1; j++)
                            {
                                InformationSquare += Math.Abs(bitmap.GetPixel(j, i + 1).R - bitmap.GetPixel(j, i).R) + Math.Abs(bitmap.GetPixel(j + 1, i).R - bitmap.GetPixel(j, i).R);
                            }
                        }
                        InformationSquareArr[h, w] = InformationSquare / temp;
                        InformationSquare = 0;
                    }
                }
            }

            NormalizedInformationSquareArr = Normalization(InformationSquareArr, HeightSquare, WidthSquare);
            for (int h = 0; h < HeightSquare; h++)
            {
                for (int w = 0; w < WidthSquare; w++)
                {
                    for (int i = h * 10; i < h * 10 + 10; i++)
                    {
                        for (int j = w * 10; j < w * 10 + 10; j++)
                        {
                            Picbitmap.SetPixel(j, i, Color.FromArgb(NormalizedInformationSquareArr[h, w], NormalizedInformationSquareArr[h, w], NormalizedInformationSquareArr[h, w]));
                        }
                    }
                }
            }

            panel.Height = 207;
            panel.Width = 207;
            if (Picbitmap.Height < 200)
                panel.Height = Picbitmap.Height + 7;
            if (Picbitmap.Width < 200)
                panel.Width = Picbitmap.Width + 7;
            pic.Height = Picbitmap.Height;
            pic.Width = Picbitmap.Width;
            pic.Image = Picbitmap;

            datagrid.Rows.Clear();
            datagrid.Columns.Clear();
            for (int w = 0; w < WidthSquare; w++)
            {
                datagrid.Columns.Add("name", null);
            }
            for (int h = 0; h < HeightSquare; h++)
            {
                datagrid.Rows.Add();
            }            
            for (int h = 0; h < HeightSquare; h++)
            {
                for (int w = 0; w < WidthSquare; w++)
                {
                    datagrid.Rows[h].Cells[w].Value = InformationSquareArr[h, w];
                }
            }

            return InformationSquareArr;
        }

        //функция нормализации
        private int[,] Normalization(int[,] Arr, int height, int width)
        {
            double max = Arr[0, 0];
            double min = Arr[0, 0];
            double newmax = 255;
            double newmin = 0;
            int[,] NormalizedArr = new int[height, width];

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (Arr[h, w] < min)
                        min = Arr[h, w];
                    if (Arr[h, w] > max)
                        max = Arr[h, w];
                }
            }
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    NormalizedArr[h, w] = (int)(((Arr[h, w] - min) * ((newmax - newmin) / (max - min))) + newmin);//формула нормализации
                }
            }
            return NormalizedArr;
        }

        Bitmap CreateGraphic(int[,] arr1, int[,] arr2, PictureBox picbox)
        {            
            Bitmap mybitmap = new Bitmap(picbox.Width, picbox.Height);
            Chart Graphic = new Chart();
            Graphic.Width = picbox.Width;
            Graphic.Height = picbox.Height;

            Graphic.ChartAreas.Add(new ChartArea("Graphic"));
            Graphic.ChartAreas[0].AxisX.Title = "Информативность";
            Graphic.ChartAreas[0].AxisY.Title = "% Несовпадений";
            
            Graphic.ChartAreas[0].AxisX.Interval = 5;

            Series Points = Graphic.Series.Add("Points");
            Points.ChartType = SeriesChartType.Point;

            Series Lines = Graphic.Series.Add("Lines");
            Lines.ChartType = SeriesChartType.Line;
            Lines.Color = Color.Red;
            Lines.BorderWidth = 2;

            int[] ForSortArr1 = new int[arr1.Length];            
            int[] ForSortArr2 = new int[arr1.Length];
            int counter = 0;
            for (int h = 0; h < arr1.GetLength(0); h++)
            {
                for (int w = 0; w < arr1.GetLength(1); w++)
                {
                    ForSortArr1[counter] = arr1[h, w];
                    ForSortArr2[counter] = arr2[h, w];
                    counter++;
                }
            }
            for (int i = 0; i < ForSortArr1.Length; i++)
                for (int j = 0; j < ForSortArr1.Length - 1; j++)
                    if (ForSortArr1[j] > ForSortArr1[j + 1])
                    {
                        int t = ForSortArr1[j + 1];
                        ForSortArr1[j + 1] = ForSortArr1[j];
                        ForSortArr1[j] = t;

                        int t2 = ForSortArr2[j + 1];
                        ForSortArr2[j + 1] = ForSortArr2[j];
                        ForSortArr2[j] = t2;
                    }
            for (int w = 0; w < ForSortArr1.Length; w++)
            {
                Points.Points.AddXY(ForSortArr1[w], ForSortArr2[w]);
                Lines.Points.AddXY(ForSortArr1[w], ForSortArr2[w]);
            }

            Graphic.DrawToBitmap(mybitmap, new Rectangle(0, 0, picbox.Width, picbox.Height));            

            return mybitmap;

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {            
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    Informationfuncktion(First, pictureBox4, dataGridView1, 100, panel4);
                    Informationfuncktion(Second, pictureBox5, dataGridView2, 100, panel5);
                    Informationfuncktion(Combined, pictureBox6, dataGridView3, 100, panel6, mode, dataGridView4);
                    break;
                case 1:
                    Informationfuncktion(First, pictureBox4, dataGridView1, 150, panel4);
                    Informationfuncktion(Second, pictureBox5, dataGridView2, 150, panel5);
                    Informationfuncktion(Combined, pictureBox6, dataGridView3, 150, panel6, mode, dataGridView4);
                    break;
                case 2:
                    Informationfuncktion(First, pictureBox4, dataGridView1, 200, panel4);
                    Informationfuncktion(Second, pictureBox5, dataGridView2, 200, panel5);
                    Informationfuncktion(Combined, pictureBox6, dataGridView3, 200, panel6, mode, dataGridView4);
                    break;
            }
        }

    }
}
