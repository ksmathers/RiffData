using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NumSharp;
using NumSharp.Utilities;
using RiffWave;

namespace ToneGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            m = new Model();
            InitializeComponent();
        }

        class Model
        {
            public NDArray X;
            public NDArray Y;
        }
        Model m;

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            float freq = 440;
            float.TryParse(textBox1.Text, out freq);
            m.X = np.linspace(0,1,16000);
            //Console.WriteLine(X["::10"].ToString());
            m.Y = np.sin(m.X * 2 * Math.PI * freq);
            UpdateGraph(chart1.Series[0], m.X, m.Y);
        }

        public void UpdateGraph(Series s, NumSharp.NDArray x, NumSharp.NDArray y)
        {
            s.Points.Clear();
            for (int i = 0; i < 1000; i++) {
                var _x = x[i].GetSingle();
                var _y = y[i].GetSingle();
                if (i % 1 == 0) {
                    //Console.WriteLine($"{i}:({_x},{_y}),");
                    s.Points.AddXY(_x, _y);
                }
            }
        }

        float[] Slice(NDArray data, int start=0, int end=-1, int step=1)
        {
            if (end < 0) end = data.shape[0] + end + 1;
            int ilen = (end - start) / step;
            var result = new float[ilen];
            var slice = data[new Slice(start, end, step)];
            for (int i = 0; i < ilen; i++) {
                result[i] = slice[i].GetSingle();
            }
            return result;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (m.Y==null) {
                MessageBox.Show("Generate some data first");
                return;
            }

            var dlg = new SaveFileDialog();
            string desktop = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dlg.InitialDirectory = desktop;
            dlg.Filter = "Wave Audio File (*.wav)|*.wav";
            if (dlg.ShowDialog() == DialogResult.OK) {
                string fpath = dlg.FileName;
                int blockSize = 80;

                WavFile wav = new WavFile(fpath, 1, 8000);

                // Demonstrate writing audio data all at once
                //var audio = Slice(m.Y);
                //wav.WriteAudio(audio);

                // Demonstrate writing audio data in blocks
                for (int i = 0; i < m.Y.shape[0]; i += blockSize) {
                    var audio = Slice(m.Y, i, i + blockSize);
                    wav.WriteAudio(audio);
                }

                // Human readable view of the WAV file
                textDump.Text = wav.Dump();

                // Binary save
                wav.Save();
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
