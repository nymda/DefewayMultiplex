using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DefewayMultiplex
{
    public partial class Form1 : Form
    {
        public string username = "admin";
        public string password = "";
        public List<String> ips = new List<String> { };
        public string selectedDirectory = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public int getCamCount(string ipi)
        {
            string ip;

            this.Invoke(new MethodInvoker(delegate ()
            {
                listBox1.Items.Insert(0, "Testing: " + ipi);
            }));

            if (ipi.Contains("https://"))
            {
                ip = ipi;
            }
            else
            {
                ip = @"http://" + ipi;
            }

            string[] ipPingArr = ip.Split(':');
            string ipPing = ipPingArr[1];
            ipPing = ipPing.Replace("//", "");
            var p = new Ping();
            PingReply reply = p.Send(ipPing, 1000);

            if (reply.Status == IPStatus.Success)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    listBox1.Items.Insert(0, "Ping success.");
                }));
                string data = "/cgi-bin/gw.cgi?xml=<juan%20ver=\"0\"><devinfo%20camcnt=\"\"/></juan>";
                WebClient w = new WebClient();
                byte[] dataByte = w.DownloadData(ip + data);
                string str = System.Text.Encoding.Default.GetString(dataByte);
                string[] parts = str.Split('\n');
                string parta = parts[1].Substring(20);
                parta = parta.Replace("></devinfo>", "");
                parta = parta.Replace("camcnt=", "");
                parta = parta.Replace("\"", "");
                int camCount = Int32.Parse(parta);
                Console.WriteLine("Cameras: " + camCount);
                doVisualShit(ip, camCount);
                return camCount;
            }
            else
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    listBox1.Items.Insert(0, "Ping failed.");
                }));
                return -1;
            }
        }

        public void doVisualShit(string ip, int camcnt)
        {
            //640 x 360

            this.Invoke(new MethodInvoker(delegate ()
            {
                listBox1.Items.Insert(0, "Camcnt: " + camcnt);
            }));

            if (camcnt == -1)
            {
                return;
            }

            WebClient dl = new WebClient();
            List<Image> snaps = new List<Image> { };
            List<Image> snapsbuffer = new List<Image> { };

            string defewaylocation1 = "/cgi-bin/snapshot.cgi?chn=";
            string defewaylocation2 = "&u=admin&p=";

            int failedSnaps = 0;

            bool saveImageFile = true;

            for (int i = 0; i < camcnt; i++)
            {
                try
                {
                    byte[] imgbytes = dl.DownloadData(ip + defewaylocation1 + i + defewaylocation2);
                    var ms = new MemoryStream(imgbytes);
                    Image cur = Image.FromStream(ms);
                    snaps.Add(cur);
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.Items.Insert(0, "Got snap " + (i + 1));
                    }));
                }
                catch (WebException e)
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.Items.Insert(0, "Failed snap " + (i + 1));
                        failedSnaps++;
                    }));
                }
            }

            if (failedSnaps == camcnt)
            {
                saveImageFile = false;
                this.Invoke(new MethodInvoker(delegate ()
                {
                    listBox1.Items.Insert(0, "Credentials likely incorrect.");
                    listBox1.Items.Insert(0, "All cams failed. Discarding.");
                }));
            }

            int len = 640;
            int hei = 360;

            foreach (Image i in snaps)
            {
                snapsbuffer.Add(new Bitmap(i, new Size(len, hei)));
            }

            snaps = snapsbuffer;

            Bitmap canvas = null;

            List<Point> allSnaPositions = null;

            switch (camcnt)
            {
                case 4:
                    canvas = new Bitmap(len * 2, hei * 2);
                    allSnaPositions = getAllPositions(2, len, hei);
                    break;
                case 8:
                    canvas = new Bitmap(len * 3, hei * 3);
                    allSnaPositions = getAllPositions(3, len, hei);
                    break;
                case 16:
                    canvas = new Bitmap(len * 4, hei * 4);
                    allSnaPositions = getAllPositions(4, len, hei);
                    break;
                case 24:
                    canvas = new Bitmap(len * 5, hei * 5);
                    allSnaPositions = getAllPositions(5, len, hei);
                    break;
            }

            Graphics gr = Graphics.FromImage(canvas);
            gr.FillRectangle(Brushes.Gray, 0, 0, canvas.Width, canvas.Height);
            int count = 0;
            foreach (Bitmap img in snaps)
            {
                gr.DrawImage(img, allSnaPositions[count]);
                count++;
            }

            Font lucFont = new Font("Lucida Console", 10);
            PointF Pointvar = new PointF(10f, 10f);
            SizeF size = gr.MeasureString(ip, lucFont);
            RectangleF rect = new RectangleF(Pointvar, size);
            gr.FillRectangle(Brushes.Black, rect);
            gr.DrawString(ip, lucFont, Brushes.White, Pointvar);
            string filename = RandomString(10);
            if (saveImageFile)
            {
                canvas.Save(selectedDirectory + "/" + filename + ".png");
                pictureBox1.Image = canvas;
                this.Invoke(new MethodInvoker(delegate ()
                {
                    listBox1.Items.Insert(0, "Saved.");
                }));
            }
        }

        public static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public List<Point> getAllPositions(int num, int _width, int _height)
        {
            List<Point> points = new List<Point> { };

            for (int o = 0; o < num; o++)
            {
                for (int i = 0; i < num; i++)
                {
                    points.Add(new Point(_width * i, _height * o));
                }
            }
            return points;
        }

        public static int GetNextInt32(RNGCryptoServiceProvider rnd)
        {
            byte[] randomInt = new byte[4];
            rnd.GetBytes(randomInt);
            return Convert.ToInt32(randomInt[0]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                RNGCryptoServiceProvider rnd2 = new RNGCryptoServiceProvider(); //may be overdoing it a little
                ips = ips.OrderBy(x => GetNextInt32(rnd2)).ToList();
            }

            Thread a = new Thread(() => threadManager());
            a.IsBackground = true;
            a.Start();
        }

        public void threadManager()
        {
            foreach (string ip in ips)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    listBox1.Items.Insert(0, "------------");
                }));
                try
                {
                    getCamCount(ip);
                }
                catch
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.Items.Insert(0, "Error in " + ip);
                    }));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Input File";
                dlg.Filter = "Text Files | *.txt";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ips = System.IO.File.ReadAllLines(dlg.FileName).ToList();
                    button2.ForeColor = Color.Green;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    selectedDirectory = fbd.SelectedPath + "/";
                    button3.ForeColor = Color.Green;
                }
            }
        }
    }
}