using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace brightness_conversion
{
    public partial class Form2 : Form
    {
        double si, gamma;

        public Form2(double si, double gamma)
        {
            InitializeComponent();
            this.si = si;
            this.gamma = gamma;
            textBox1.Text = si.ToString();
            textBox2.Text = gamma.ToString();
        }

        public double Si
        {
            get { return si; }
        }

        public double Gamma
        {
            get { return gamma; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            double tmpSi, tmpGamma;

            if (Double.TryParse(textBox1.Text, out tmpSi))
                si = tmpSi;
            if (Double.TryParse(textBox2.Text, out tmpGamma))
                gamma = tmpGamma;

            Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
