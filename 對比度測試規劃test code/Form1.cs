using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace 對比度測試規劃test_code
{
    public partial class Form1 : Form
    {
        double 圖像對比數值 = 1;
        int 滑條數值 = 0;
        int 對比度;
        bool IsCountDone = true;
        public delegate Bitmap MethodCaller(Bitmap bitmap);//定義個代理
        public delegate Bitmap 對比度MethodCaller(Bitmap bitmap, decimal v);//定義個代理 

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            trackBar1.Minimum = -10;
            trackBar1.Maximum = 10;
            trackBar1.Value = 0;
            trackBar2.Maximum = 10;
            trackBar2.Minimum = -10;
            trackBar2.Value = 0;
            Form.CheckForIllegalCrossThreadCalls = false;
        }
        #region 灰階圖像對比度提升測試碼,暫不啟用
        public static Bitmap RGB2Gray(Bitmap srcBitmap)
        {
            int wide = srcBitmap.Width;
            int height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, wide, height);
            //将Bitmap锁定到系统内存中,获得BitmapData
            BitmapData srcBmData = srcBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            //创建Bitmap
            Bitmap dstBitmap = CreateGrayscaleImage(wide, height);//这个函数在后面有定义
            BitmapData dstBmData = dstBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
            System.IntPtr srcPtr = srcBmData.Scan0;
            System.IntPtr dstPtr = dstBmData.Scan0;
            //将Bitmap对象的信息存放到byte数组中
            int src_bytes = srcBmData.Stride * height;
            byte[] srcValues = new byte[src_bytes];
            int dst_bytes = dstBmData.Stride * height;
            byte[] dstValues = new byte[dst_bytes];
            //复制GRB信息到byte数组
            System.Runtime.InteropServices.Marshal.Copy(srcPtr, srcValues, 0, src_bytes);
            System.Runtime.InteropServices.Marshal.Copy(dstPtr, dstValues, 0, dst_bytes);
            //根据Y=0.299*R+0.114*G+0.587B,Y为亮度
            for (int i = 0; i < height; i++)
                for (int j = 0; j < wide; j++)
                {
                    //只处理每行中图像像素数据,舍弃未用空间
                    //注意位图结构中RGB按BGR的顺序存储
                    int k = 3 * j;
                    byte temp = (byte)(srcValues[i * srcBmData.Stride + k + 2] * .299 + srcValues[i * srcBmData.Stride + k + 1] * .587 + srcValues[i * srcBmData.Stride + k] * .114);
                    dstValues[i * dstBmData.Stride + j] = temp;
                }
            System.Runtime.InteropServices.Marshal.Copy(dstValues, 0, dstPtr, dst_bytes);
            //解锁位图
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return dstBitmap;
        }
        #endregion
        ///<summary>
        /// Create and initialize grayscale image
        ///</summary>
        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            // create new image
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            // set palette to grayscale
            SetGrayscalePalette(bmp);
            // return new image
            return bmp;
        }//#
        ///<summary>
        /// Set pallete of the image to grayscale
        ///</summary>

        public static void SetGrayscalePalette(Bitmap srcImg)
        {
            // check pixel format
            if (srcImg.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException();
            // get palette
            ColorPalette cp = srcImg.Palette;
            // init palette
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }
            srcImg.Palette = cp;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (IsCountDone == true)
            {
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                if (pictureBox3.Image != null)
                {
                    pictureBox3.Image.Dispose();
                }

                Bitmap bitmap;
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;//該值確定是否可以選擇多個檔案
                dialog.Title = "請選擇資料夾";
                dialog.Filter = "所有檔案(*.*)|*.*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bitmap = new Bitmap(dialog.FileName);
                    this.pictureBox1.Image = bitmap;
                }
            }
            else
            {
                MessageBox.Show("圖片運算中,請勿更換圖片");
            }
        }

        //定義對比度調整函式
        private Bitmap ContrastP(Bitmap a, decimal v)
        {
            //鎖定指定圖像像素值
            System.Drawing.Imaging.BitmapData bmpData = a.LockBits(new Rectangle(0, 0, a.Width, a.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            int bytes = a.Width * a.Height * 3;
            IntPtr ptr = bmpData.Scan0;
            int stride = bmpData.Stride;
            unsafe
            {
                byte* p = (byte*)ptr.ToPointer();
                int temp;
                for (int j = 0; j < a.Height; j++)
                {
                    for (int i = 0; i < a.Width * 3; i++)
                    {
                        temp = (int)((p[0] - 127) * v + 127);
                        temp = (temp > 255) ? 255 : temp < 0 ? 0 : temp;
                        p[0] = (byte)temp;
                        label1.Text = "目前對比度" + temp;
                        p++;
                    }
                    p += stride - a.Width * 3;
                }
                //Parallel.For(0, a.Height, j =>
                //{
                //    Parallel.For(0, a.Width * 3, i =>
                //    {
                //        temp = (int)((p[0] - 127) * v + 127);
                //        temp = (temp > 255) ? 255 : temp < 0 ? 0 : temp;
                //        p[0] = (byte)temp;
                //        Console.WriteLine("取值" + temp);
                //        p++;
                //    });
                //    p += stride - a.Width * 3;
                //});
                //Parallel.For(0, a.Height, j =>
                //{
                //    Parallel.For(0, a.Width * 3, i =>
                //      {
                //          temp = (int)((p[0] - 127) * v + 127);
                //          temp = (temp > 255) ? 255 : temp < 0 ? 0 : temp;
                //          p[0] = (byte)temp;
                //          //label1.Text = "目前對比度" + temp;
                //          p++;
                //      });
                //    p += stride - a.Width * 3;
                //});
            }
            IsCountDone = true;
            a.UnlockBits(bmpData);
            return a;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (IsCountDone == true)
            {
                IsCountDone = false;
                Thread 白平衡執行序 = new Thread(new ThreadStart(delegate
                {
                    MethodCaller mc = new MethodCaller(AutoWhiteBalance);
                    if (pictureBox3.Image != null)
                    {
                        Bitmap bm = new Bitmap(pictureBox3.Image);
                        IAsyncResult result = mc.BeginInvoke(bm, null, null);
                        Bitmap bm1 = mc.EndInvoke(result);//用於接收返回值 
                        pictureBox3.Image = bm1;
                    }
                    else if (pictureBox1.Image != null)
                    {

                        Bitmap bm = new Bitmap(pictureBox1.Image);
                        IAsyncResult result = mc.BeginInvoke(bm, null, null);
                        Bitmap bm1 = mc.EndInvoke(result);//用於接收返回值 
                        pictureBox3.Image = bm1;
                        //AutoWhiteBalance(bm);
                        //pictureBox3.Image = bm;
                    }
                    else
                    {
                        MessageBox.Show("尚未選擇來源圖檔");
                    }
                }));
                白平衡執行序.Start();

            }
            else
            {
                MessageBox.Show("圖片運算尚未完成,請稍後");
            }

            //Bitmap bm = new Bitmap(pictureBox1.Image);
            //IAsyncResult result = mc.BeginInvoke(bm, null, null);
            //Bitmap bm1 = mc.EndInvoke(result);//用於接收返回值 
            //pictureBox3.Image = bm1;

            //if(pictureBox3.Image != null)
            //{
            //    pictureBox3.Image.Dispose();
            //}
            //if(pictureBox1.Image != null)
            //{
            //    Bitmap bitmap;
            //    bitmap = new Bitmap(pictureBox1.Image);
            //    //ContrastP(bitmap, 圖像對比數值);
            //    pictureBox3.Image = bitmap;
            //}
            //else
            //{
            //    MessageBox.Show("尚未選擇來源圖檔");
            //}

        }



        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            if (IsCountDone == true)
            {
                IsCountDone = false;
                Thread 白平衡執行序 = new Thread(new ThreadStart(delegate
                {
                    MethodCaller mc = new MethodCaller(AutoWhiteBalance);
                    Bitmap bm = new Bitmap(pictureBox1.Image);
                    IAsyncResult result = mc.BeginInvoke(bm, null, null);
                    Bitmap bm1 = mc.EndInvoke(result);//用於接收返回值
                    pictureBox3.Image = bm1;
                }));
                白平衡執行序.Start();
                IsCountDone = true;
            }
            else
            {
                MessageBox.Show("圖片運算尚未完成,請稍後");
            }

            //trackBar1.Value = 滑條數值;
            //int 滑條數值;
            //滑條數值 = trackBar2.Value-255;
            ////圖像對比數值 = 滑條數值;
            //if (pictureBox3.Image != null)
            //{
            //    Bitmap bitmap;
            //    bitmap = new Bitmap(pictureBox3.Image);
            //    //LDPic(bitmap, -1);
            //    //LightUpPic(bitmap,1);
            //    AutoWhiteBalance(bitmap);
            //    pictureBox3.Image.Dispose();
            //    pictureBox3.Image = bitmap;
            //}
            //else if (pictureBox1.Image != null)
            //{
            //    Bitmap bitmap;
            //    bitmap = new Bitmap(pictureBox1.Image);
            //    //LDPic(bitmap, -1);
            //    //LightUpPic(bitmap,1);
            //    AutoWhiteBalance(bitmap);
            //    pictureBox3.Image = bitmap;
            //}
            //else
            //{
            //    MessageBox.Show("尚未選擇來源圖檔");
            //}

        }


        #region 調整光暗測試碼,暫不啟用
        /// <summary>
        /// 調整光暗
        /// </summary>
        /// <param name="mybm">原始圖片</param>
        /// <param name="width">原始圖片的長度</param>
        /// <param name="height">原始圖片的高度</param>
        /// <param name="val">增加或減少的光暗值</param>
        public Bitmap LDPic(Bitmap mybm, int val)//亮度測試
        {
            Bitmap bm = new Bitmap(mybm.Width, mybm.Height);//初始化一個記錄經過處理後的圖片物件
            //int x, y, resultR, resultG, resultB;//x、y是迴圈次數，後面三個是記錄紅綠藍三個值的
            //Color pixel;
            int[,,] rgbData = getRGBData_unsafe(mybm);
            int Width = rgbData.GetLength(0);
            int Height = rgbData.GetLength(1);
            // Step 2: 增加亮度 30
            int g;
            unsafe
            {
                //for (x = 0; x < mybm.Width; x++)
                //{
                //    for (y = 0; y < mybm.Height; y++)
                //    {
                //        pixel = mybm.GetPixel(x, y);//獲取當前畫素的值
                //        resultR = pixel.R + val;//檢查紅色值會不會超出[0, 255]
                //        resultG = pixel.G + val;//檢查綠色值會不會超出[0, 255]
                //        resultB = pixel.B + val;//檢查藍色值會不會超出[0, 255]
                //        bm.SetPixel(x, y, Color.FromArgb(resultR, resultG, resultB));//繪圖
                //    }
                //}
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            g = rgbData[x, y, c];
                            g += 30;
                            if (g > 255)
                                g = 255;
                            rgbData[x, y, c] = g;
                            bm.SetPixel(x, y, Color.FromArgb(x, y, c));
                        }
                    }
                }
            }
            return bm;
        }
        #endregion
        #region 亮度提升測試碼,暫不啟用
        public Bitmap LightUpPic(Bitmap mybm, int val)//亮度測試碼
        {
            Bitmap bm = new Bitmap(mybm.Width, mybm.Height);//初始化一個記錄經過處理後的圖片物件
            int w = mybm.Width;
            int h = mybm.Height;
            Int32 total = 0;
            int[,] imageR = new int[w, h];
            int[,] imageG = new int[w, h];
            int[,] imageB = new int[w, h];
            double[,] imageGray = new double[w, h];
            double[] GrayValue = new double[307200];

            //抓每個點的RGB值與灰階值
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    imageR[i, j] = mybm.GetPixel(i, j).R;
                    imageG[i, j] = mybm.GetPixel(i, j).G;
                    imageB[i, j] = mybm.GetPixel(i, j).B;

                    imageGray[i, j] = 0.299 * imageR[i, j] + 0.587 * imageG[i, j] + 0.114 * imageB[i, j];
                }
            }
            for (int i = 0; i < 307200; i++)
            {
                GrayValue[i] = imageGray[i % 640, i / 640];
            }
            Array.Sort(GrayValue);
            for (int i = 291840; i < 307200; i++)
            {
                total += Convert.ToInt16(GrayValue[i]);
            }
            total /= 15360;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    imageR[i, j] += imageR[i, j] * (total / 255);
                    imageG[i, j] += imageG[i, j] * (total / 255);
                    imageB[i, j] += imageB[i, j] * (total / 255);
                }
            }
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    mybm.SetPixel(i, j, Color.FromArgb(imageR[i, j], imageG[i, j], imageB[i, j]));
                }
            }
            return mybm;
        }
        #endregion
        #region 亮度提升測試碼,暫不啟用
        private void button7_Click(object sender, EventArgs e)//不啟用
        {
            // Step 1: 取出顏色資料
            Bitmap bitmap;
            bitmap = new Bitmap(pictureBox1.Image);
            int[,,] rgbData = getRGBData_unsafe(bitmap);
            int Width = rgbData.GetLength(0);
            int Height = rgbData.GetLength(1);
            // Step 2: 增加亮度 30
            int g;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        g = rgbData[x, y, c];
                        g += 30;
                        if (g > 255)
                            g = 255;
                        rgbData[x, y, c] = g;
                    }
                }
            }
            // Step 3: 將處理後的資料寫回 CurrentImage
            //setRGBData_unsafe(rgbData);
        }

        public int[,,] getRGBData_unsafe(Bitmap image)
        {
            Bitmap bimage = new Bitmap(image);
            return getRGBData(bimage);
        }

        public int[,,] getRGBData(Bitmap image)
        {
            // Step 1: 利用 Bitmap 將 image 包起來
            Bitmap bimage = new Bitmap(image);
            int Height = bimage.Height;
            int Width = bimage.Width;
            int[,,] rgbData = new int[Width, Height, 3];
            // Step 2: 取得像點顏色資訊
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color color = bimage.GetPixel(x, y);
                    rgbData[x, y, 0] = color.R;
                    rgbData[x, y, 1] = color.G;
                    rgbData[x, y, 2] = color.B;
                }
            }
            return rgbData;
        }
        #endregion
        private Bitmap AutoWhiteBalance(Bitmap img)
        {
            int pixelsize = img.Width * img.Height;

            double[,,] YCbCr = new double[img.Width, img.Height, 3];
            double Mr = 0, Mb = 0, Ymax = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    double[] YCbCr_value = toYCbCr(img.GetPixel(i, j));
                    for (int k = 0; k < 3; k++)
                        YCbCr[i, j, k] = YCbCr_value[k];
                    Mr += YCbCr[i, j, 2];
                    Mb += YCbCr[i, j, 1];
                    Ymax = Math.Max(Ymax, YCbCr[i, j, 0]);
                }
            }
            //Parallel.For(0, img.Width, i =>
            //{
            //    Parallel.For(0, img.Height, j =>
            //    {
            //        double[] YCbCr_value = toYCbCr(img.GetPixel(i, j));
            //        for (int k = 0; k < 3; k++)
            //            YCbCr[i, j, k] = YCbCr_value[k];
            //        Mr += YCbCr[i, j, 2];
            //        Mb += YCbCr[i, j, 1];
            //        Ymax = Math.Max(Ymax, YCbCr[i, j, 0]);
            //    });
            //});

            Mr /= pixelsize;
            Mb /= pixelsize;

            double Dr = 0, Db = 0;
            for (int i = 0; i < YCbCr.GetLength(0); i++)
            {
                for (int j = 0; j < YCbCr.GetLength(1); j++)
                {
                    Db += Math.Pow((YCbCr[i, j, 1] - Mb), 2);
                    Dr += Math.Pow((YCbCr[i, j, 2] - Mr), 2);
                }
            }
            //Parallel.For(0, YCbCr.GetLength(0), i =>
            //{
            //    Parallel.For(0, YCbCr.GetLength(1), j =>
            //    {
            //        Db += Math.Pow((YCbCr[i, j, 1] - Mb), 2);
            //        Dr += Math.Pow((YCbCr[i, j, 2] - Mr), 2);
            //    });
            //});
            //Dr /= pixelsize;
            //Db /= pixelsize;


            double[,] Y = new double[img.Width, img.Height];
            double[] Yhistogram = new double[256];
            double Ysum = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    int value = (Math.Abs(YCbCr[i, j, 1] - (Mb + Db * Math.Sign(Mb))) < 1.5 * Db) && (Math.Abs(YCbCr[i, j, 2]) - (1.5 * Mr + Dr * Math.Sign(Mr))) < 1.5 * Dr ? 1 : 0;
                    if (value <= 0)
                        continue;
                    double y = YCbCr[i, j, 0];
                    Y[i, j] = y;
                    Yhistogram[(int)Y[i, j]]++;
                    Ysum++;
                }
            }
            //Parallel.For(0, img.Width, i =>
            //{
            //    Parallel.For(0, img.Height, j =>
            //    {
            //        int value = (Math.Abs(YCbCr[i, j, 1] - (Mb + Db * Math.Sign(Mb))) < 1.5 * Db) && (Math.Abs(YCbCr[i, j, 2]) - (1.5 * Mr + Dr * Math.Sign(Mr))) < 1.5 * Dr ? 1 : 0;
            //        //if (value <= 0) continue;
            //        double y = YCbCr[i, j, 0];
            //        Y[i, j] = y;
            //        Yhistogram[(int)Y[i, j]]++;
            //        Ysum++;
            //    });
            //});

            double Yhistogramsum = 0;
            double Ymin = 0;
            for (int i = Yhistogram.Count() - 1; i >= 0; i--)
            {
                Yhistogramsum += Yhistogram[i];
                if (Yhistogramsum > 0.1 * Ysum)
                {
                    Ymin = i;
                    break;
                }
            }


            double Raver = 0, Gaver = 0, Baver = 0;
            double averSum = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (Y[i, j] > Ymin)
                    {

                        Color color = img.GetPixel(i, j);
                        int r = color.R;
                        int g = color.G;
                        int b = color.B;
                        Raver += r;
                        Gaver += g;
                        Baver += b;
                        averSum++;
                    }
                }
            }
            //Parallel.For(0, img.Width, i =>
            //{
            //    Parallel.For(0, img.Height, j =>
            //    {
            //        if (Y[i, j] > Ymin)
            //        {
            //            Color color = img.GetPixel(i, j);
            //            int r = color.R;
            //            int g = color.G;
            //            int b = color.B;
            //            Raver += r;
            //            Gaver += g;
            //            Baver += b;
            //            averSum++;
            //        }
            //    });
            //});
            Raver /= averSum;
            Gaver /= averSum;
            Baver /= averSum;

            double Rgain = Ymax / Raver, Ggain = Ymax / Gaver, Bgain = Ymax / Baver;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    Color color = img.GetPixel(i, j);
                    int r = ensureColor((int)Math.Floor(color.R * Rgain));
                    int g = ensureColor((int)Math.Floor(color.G * Ggain));
                    int b = ensureColor((int)Math.Floor(color.B * Bgain));
                    img.SetPixel(i, j, Color.FromArgb(r, g, b));
                }
            }
            //Parallel.For(0, img.Width, i =>
            //{
            //    Parallel.For(0, img.Height, j =>
            //    {
            //        Color color = img.GetPixel(i, j);
            //        int r = ensureColor((int)Math.Floor(color.R * Rgain));
            //        int g = ensureColor((int)Math.Floor(color.G * Ggain));
            //        int b = ensureColor((int)Math.Floor(color.B * Bgain));
            //        img.SetPixel(i, j, Color.FromArgb(r, g, b));
            //    });
            //});
            IsCountDone = true;
            return img;
        }

        private double[] toYCbCr(System.Drawing.Color color)
        {
            int r = color.R;
            int g = color.G;
            int b = color.B;

            double Y = 0.299 * r + 0.587 * g + 0.114 * b;
            double Cb = 0.5 * r - 0.419 * g - 0.081 * b;
            double Cr = -0.169 * r - 0.331 * g + 0.5 * b;

            return new double[] { Y, Cb, Cr };
        }

        private int ensureColor(double color)
        {
            if (color < 0)
                return 0;
            if (color > 255)
                return 255;
            return (int)color;
        }

        private void trackBar1_Scroll(object sender, MouseEventArgs e)
        {
            if (IsCountDone == true)
            {
                IsCountDone = false;
                decimal 滑條數值 = (decimal)trackBar1.Value / 10;

                Thread 對比度執行序 = new Thread(new ThreadStart(delegate
                {
                    if (pictureBox3.Image != null)
                    {
                        對比度MethodCaller mc1 = new 對比度MethodCaller(ContrastP);
                        Bitmap bm = new Bitmap(pictureBox3.Image);
                        IAsyncResult result = mc1.BeginInvoke(bm, 滑條數值, null, null);
                        Bitmap bm1 = mc1.EndInvoke(result);//用於接收返回值 
                        pictureBox3.Image = bm1;

                    }
                    else if (pictureBox1.Image != null)
                    {
                        對比度MethodCaller mc1 = new 對比度MethodCaller(ContrastP);
                        Bitmap bm = new Bitmap(pictureBox1.Image);
                        IAsyncResult result = mc1.BeginInvoke(bm, 滑條數值, null, null);
                        Bitmap bm1 = mc1.EndInvoke(result);//用於接收返回值 
                        pictureBox3.Image = bm1;

                    }
                    else
                    {
                        MessageBox.Show("尚未選擇來源圖檔");
                    }
                    //對比度MethodCaller mc1 = new 對比度MethodCaller(ContrastP);
                    //Bitmap bm = new Bitmap(pictureBox1.Image);
                    //IAsyncResult result = mc1.BeginInvoke(bm, 滑條數值, null, null);
                    //Bitmap bm1 = mc1.EndInvoke(result);//用於接收返回值 
                    //pictureBox3.Image = bm1;
                }));
                對比度執行序.Start();

                //Bitmap bmp = new Bitmap(pictureBox1.Image);
                //System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                //int bytes = bmp.Width * bmp.Height * 3;
                //IntPtr ptr = bmpData.Scan0;
                //int stride = bmpData.Stride;
                //unsafe
                //{
                //    byte* p = (byte*)ptr;
                //    int temp;
                //    for (int j = 0; j < bmp.Height; j++)
                //    {
                //        for (int i = 0; i < bmp.Width * 3; i++)
                //        {
                //            temp = (int)((p[0] - 127) * 滑條數值 + 127);
                //            temp = (temp > 255) ? 255 : temp < 0 ? 0 : temp;
                //            label1.Text = "目前對比度:" + temp;
                //        }
                //    }
                //}
            }
            else
            {
                MessageBox.Show("圖片運算尚未完成,請稍後");
            }
        }


    }
}
