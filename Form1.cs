using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace brightness_conversion
{
    public partial class Form1 : Form
    {
        private Bitmap bm;
        private Form2 form2;
        private double si, gamma;
        private bool isLoaded;
        private const int threshold = 700;

        public Form1()
        {
            InitializeComponent();
            si = 1;
            gamma = 2.0;
            form2 = new Form2(si, gamma);
            isLoaded = false;
            transformToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Enabled = false;
            clearToolStripMenuItem.Enabled = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = System.IO.Path.Combine(Application.StartupPath, "Images");

            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            bm = Bitmap.FromFile(openFileDialog1.FileName) as Bitmap;
            pictureBox1.Image = bm;

            this.Size = new Size(bm.Width + 16, bm.Height + 62);
            this.CenterToScreen();

            isLoaded = true;
            transformToolStripMenuItem.Enabled = true;
            clearToolStripMenuItem.Enabled = true;
        }

        private void correctionWithReferenceColor(double r, double g, double b)
        {
            Bitmap recieved = new Bitmap(bm.Width, bm.Height);
            BitmapData initialData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite,
                                PixelFormat.Format32bppRgb);
            BitmapData recievedData = recieved.LockBits(new Rectangle(0, 0, recieved.Width, recieved.Height), ImageLockMode.ReadWrite,
                                PixelFormat.Format32bppArgb);
            IntPtr initialPtr = initialData.Scan0;
            IntPtr recievedPtr = recievedData.Scan0;
            int initialBytes = Math.Abs(initialData.Stride) * bm.Height;
            int recievedBytes = Math.Abs(recievedData.Stride) * recieved.Height;
            byte[] initialValues = new byte[initialBytes];
            byte[] recievedValues = new byte[recievedBytes];

            System.Runtime.InteropServices.Marshal.Copy(initialPtr, initialValues, 0, initialBytes);
            System.Runtime.InteropServices.Marshal.Copy(recievedPtr, recievedValues, 0, recievedBytes);

            int col;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(initialValues[i] * b)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(initialValues[i + 1] * g)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(initialValues[i + 2] * r)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }

            System.Runtime.InteropServices.Marshal.Copy(initialValues, 0, initialPtr, initialBytes);
            System.Runtime.InteropServices.Marshal.Copy(recievedValues, 0, recievedPtr, recievedBytes);

            bm.UnlockBits(initialData);
            recieved.UnlockBits(recievedData);

            pictureBox1.Image = recieved.Clone() as Bitmap;
            pictureBox1.Refresh();

            resetToolStripMenuItem.Enabled = true;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (isLoaded && colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Color refColor = colorDialog1.Color;
                Color pixel = bm.GetPixel(e.X, e.Y);

                if (Math.Abs(refColor.R - pixel.R) > 0 || Math.Abs(refColor.G - pixel.G) > 0 ||
                    Math.Abs(refColor.B - pixel.B) > 0)
                {
                    correctionWithReferenceColor((double)refColor.R / pixel.R, (double)refColor.G / pixel.G,
                                                 (double)refColor.B / pixel.B);
                }
            }
        }

        private void grayWorldCorrection(byte[] initialValues, ref byte[] recievedValues)
        {
            int n = bm.Width * bm.Height;

            double avgR = 0, avgG = 0, avgB = 0;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                avgB += initialValues[i];
                avgG += initialValues[i + 1];
                avgR += initialValues[i + 2];
            }

            avgR /= n;
            avgG /= n;
            avgB /= n;

            double avg = (avgR + avgG + avgB) / 3;

            int col;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(initialValues[i] * avg / avgB)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(initialValues[i + 1] * avg / avgG)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(initialValues[i + 2] * avg / avgR)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }
        }

        private double gammaFunc(double x)
        {
            return si * Math.Pow(x, gamma);
        }

        private void gammaFuncCorrection(byte[] initialValues, ref byte[] recievedValues)
        {
            si = form2.Si;
            gamma = form2.Gamma;

            double step = (Math.Pow(255, gamma)) / 255;
            int col;
            
            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(gammaFunc(initialValues[i]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(gammaFunc(initialValues[i + 1]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(gammaFunc(initialValues[i + 2]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }
        }

        private double logarithmFunc(double x)
        {
            return si * Math.Log(1 + x, 10);
        }

        private void logarithmicFuncCorrection(byte[] initialValues, ref byte[] recievedValues)
        {
            si = form2.Si;

            double step = (Math.Log(1 + 255, 10)) / 255;
            int col;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(logarithmFunc(initialValues[i]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(logarithmFunc(initialValues[i + 1]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(logarithmFunc(initialValues[i + 2]) / step)) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }
        }

        private void buildHyst(byte[] initialValues, ref int[] rHist, ref int[] gHist, ref int[] bHist)
        {
            for (int i = 0; i < initialValues.Length; i += 4)
            {
                rHist[initialValues[i + 2]]++;
                gHist[initialValues[i + 1]]++;
                bHist[initialValues[i]]++;
            }
        }

        private void getMinMax(int[] rHist, int[] gHist, int[] bHist, ref int rMin, ref int rMax,
                               ref int gMin, ref int gMax, ref int bMin, ref int bMax)
        {
            bool rMinFlag = false, gMinFlag = false, bMinFlag = false;

            for (int i = 0; i < 256; i++)
            {
                if (rHist[i] > threshold)
                {
                    if (!rMinFlag)
                    {
                        rMin = i;
                        rMinFlag = true;
                    }

                    rMax = i;
                }

                if (gHist[i] > threshold)
                {
                    if (!gMinFlag)
                    {
                        gMin = i;
                        gMinFlag = true;
                    }

                    gMax = i;
                }

                if (bHist[i] > threshold)
                {
                    if (!bMinFlag)
                    {
                        bMin = i;
                        bMinFlag = true;
                    }

                    bMax = i;
                }
            }
        }

        private double normalizationFunc(int pixel, int min, int max)
        {
            return (double)((pixel - min) * 255) / (max - min);
        }

        private void normalizationCorrection(byte[] initialValues, ref byte[] recievedValues)
        {
            int[] rHist = new int[256];
            int[] gHist = new int[256];
            int[] bHist = new int[256];

            buildHyst(initialValues, ref rHist, ref gHist, ref bHist);

            int rMin = 0, rMax = 255, gMin = 0, gMax = 255, bMin = 0, bMax = 255;

            getMinMax(rHist, gHist, bHist, ref rMin, ref rMax, ref gMin, ref gMax, ref bMin, ref bMax);

            int col;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(normalizationFunc(initialValues[i], bMin, bMax))) < 0 ? (byte)0 : col > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(normalizationFunc(initialValues[i + 1], gMin, gMax))) < 0 ? (byte)0 : col > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(normalizationFunc(initialValues[i + 2], rMin, rMax))) < 0 ? (byte)0 : col > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }
        }

        private void equalizationCorrection(byte[] initialValues, ref byte[] recievedValues)
        {
            int n = bm.Width * bm.Height;

            int[] rHist = new int[256];
            int[] gHist = new int[256];
            int[] bHist = new int[256];

            buildHyst(initialValues, ref rHist, ref gHist, ref bHist);

            double rCurSum = 0, gCurSum = 0, bCurSum = 0;

            double[] rSum = new double[256];
            double[] gSum = new double[256];
            double[] bSum = new double[256];

            for (int i = 0; i < 256; i++)
            {
                rCurSum += (double)rHist[i] / n;
                gCurSum += (double)gHist[i] / n;
                bCurSum += (double)bHist[i] / n;

                rSum[i] = rCurSum * 255;
                gSum[i] = gCurSum * 255;
                bSum[i] = bCurSum * 255;
            }

            int col;

            for (int i = 0; i < initialValues.Length; i += 4)
            {
                recievedValues[i] = (col = (int)(bSum[initialValues[i]])) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 1] = (col = (int)(gSum[initialValues[i + 1]])) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 2] = (col = (int)(rSum[initialValues[i + 2]])) > 255 ? (byte)255 : (byte)col;
                recievedValues[i + 3] = 255;
            }
        }

        private void transform(object sender, EventArgs e)
        {
            Bitmap recieved = new Bitmap(bm.Width, bm.Height);
            BitmapData initialData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite,
                                PixelFormat.Format32bppRgb);
            BitmapData recievedData = recieved.LockBits(new Rectangle(0, 0, recieved.Width, recieved.Height), ImageLockMode.ReadWrite,
                                PixelFormat.Format32bppArgb);
            IntPtr initialPtr = initialData.Scan0;
            IntPtr recievedPtr = recievedData.Scan0;
            int initialBytes = Math.Abs(initialData.Stride) * bm.Height;
            int recievedBytes = Math.Abs(recievedData.Stride) * recieved.Height;
            byte[] initialValues = new byte[initialBytes];
            byte[] recievedValues = new byte[recievedBytes];

            System.Runtime.InteropServices.Marshal.Copy(initialPtr, initialValues, 0, initialBytes);
            System.Runtime.InteropServices.Marshal.Copy(recievedPtr, recievedValues, 0, recievedBytes);

            string name = (sender as ToolStripMenuItem).Name;

            if (name == "grayWorldToolStripMenuItem")
                grayWorldCorrection(initialValues, ref recievedValues);
            else if (name == "gammaCorrectionToolStripMenuItem")
                gammaFuncCorrection(initialValues, ref recievedValues);
            else if (name == "logarithmicCorrectionToolStripMenuItem")
                logarithmicFuncCorrection(initialValues, ref recievedValues);
            else if (name == "normalizationToolStripMenuItem")
                normalizationCorrection(initialValues, ref recievedValues);
            else if (name == "equalizationToolStripMenuItem")
                equalizationCorrection(initialValues, ref recievedValues);

            System.Runtime.InteropServices.Marshal.Copy(initialValues, 0, initialPtr, initialBytes);
            System.Runtime.InteropServices.Marshal.Copy(recievedValues, 0, recievedPtr, recievedBytes);

            bm.UnlockBits(initialData);
            recieved.UnlockBits(recievedData);

            pictureBox1.Image = recieved.Clone() as Bitmap;
            pictureBox1.Refresh();

            resetToolStripMenuItem.Enabled = true;
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = bm;

            resetToolStripMenuItem.Enabled = false;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Graphics g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            g.Dispose();
            pictureBox1.Image = bm;
            pictureBox1.Invalidate();

            transformToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Enabled = false;
            clearToolStripMenuItem.Enabled = false;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            form2.Show();
        }
    }
}
