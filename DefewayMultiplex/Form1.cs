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

        public string defewaylocation1 = "/cgi-bin/snapshot.cgi?chn=";
        public string defewaylocation2 = "&u=admin&p=";

        public string avtechlocation = "/cgi-bin/guest/Video.cgi?media=JPEG";

        public string title = "Defeway multiplexer";
        public string status = "idle";

        public int completed = 0;
        public int total = 0;

        public Form1()
        {
            InitializeComponent();
        }

        public class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var req = base.GetWebRequest(address);
                req.Timeout = 2500;
                return req;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void setTitle()
        {
            if(completed > 0)
            {
                status = completed + "/" + total;
            }
            else
            {
                status = "idle";
            }

            this.Text = title + " | " + status;
        }

        public int getCamCount(string ipi, bool isAvtechMode)
        {
            string ip;

            this.Invoke(new MethodInvoker(delegate ()
            {
                completed++;
                setTitle();
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
                if (!isAvtechMode)
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
                    doVisualShit(ip, camCount, isAvtechMode);
                    return camCount;
                }
                else
                {
                    try
                    {
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            listBox1.Items.Insert(0, "Ping success.");
                        }));
                        string data = "/cgi-bin/nobody/Machine.cgi?action=get_capability";
                        WebClient w = new WebClient();
                        w.Credentials = new NetworkCredential("admin", "admin");
                        byte[] dataByte = w.DownloadData(ip + data);
                        string str = System.Text.Encoding.Default.GetString(dataByte);
                        string[] arrayItems = str.Split('\n');
                        string camCountObject = "";
                        foreach(string s in arrayItems)
                        {
                            Console.WriteLine(s);
                            if (s.Contains("Video.Local.Input.Num"))
                            {
                                camCountObject = s;
                            }
                        }
                        Console.WriteLine(camCountObject);
                        string[] camCountSplitObject = camCountObject.Split('=');
                        int camCount = Int32.Parse(camCountSplitObject[1]);
                        doVisualShit(ip, camCount, isAvtechMode);
                        return camCount;
                    }
                    catch
                    {
                        //remote server probably shit its self, return 4 to be safe
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            listBox1.Items.Insert(0, "Failure during camcount get.");
                        }));
                        doVisualShit(ip, 4, isAvtechMode);
                        return 4;
                    }

                }
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

        public void doVisualShit(string ip, int camcnt, bool isAvtechMode)
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
            dl.Credentials = new NetworkCredential("admin", "admin");
            List<Image> snaps = new List<Image> { };
            List<Image> snapsbuffer = new List<Image> { };

            int failedSnaps = 0;

            bool saveImageFile = true;

            bool shouldBreakOnLoop = false;

            for (int i = 0; i < camcnt; i++){
                try
                {
                    byte[] imgbytes = null;

                    if (!isAvtechMode) //is Defeway
                    {
                        imgbytes = dl.DownloadData(ip + defewaylocation1 + i + defewaylocation2);
                    }

                    else //is avtech
                    {
                        //wack serial shit bruv
                        try
                        {
                            string commandUrl = "/cgi-bin/user/Serial.cgi?action=write&device=MASTER&data=";
                            string[] chCmdArr = { "", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F", "40", "41", "42", "43", "44", "45", "46" };
                            string commandString = "02%20" + chCmdArr[i] + "%2000%2000%2023";
                            string finalCommandIrl = commandUrl + commandString;
                            byte[] sendCommand = dl.DownloadData(ip + finalCommandIrl);
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                listBox1.Items.Insert(0, "Sent serial command.");
                            }));
                        }
                        catch
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                listBox1.Items.Insert(0, "Will only download channel 0.");
                                listBox1.Items.Insert(0, "Failed to send serial command.");
                            }));
                            shouldBreakOnLoop = true;
                        }

                        imgbytes = dl.DownloadData(ip + avtechlocation);
                    }

                    var ms = new MemoryStream(imgbytes);
                    Image cur = Image.FromStream(ms);
                    snaps.Add(cur);
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.Items.Insert(0, "Got snap " + (i + 1));
                    }));

                    if (shouldBreakOnLoop)
                    {
                        break;
                    }
                }
                catch(WebException e)
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.Items.Insert(0, "Failed snap " + (i + 1));
                        failedSnaps++;
                    }));

                    if (shouldBreakOnLoop)
                    {
                        break;
                    }
                }
            }

            if(failedSnaps == camcnt)
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

            foreach(Image i in snaps)
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
            foreach(Bitmap img in snaps)
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
            radioButton1.Enabled = false;
            radioButton2.Enabled = false;

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
                    getCamCount(ip, radioButton2.Checked);
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
                    total = ips.Count();
                    button4.Enabled = true;
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                title = "Defeway multiplexer";
                setTitle();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                title = "Avtech multiplexer";
                setTitle();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RNGCryptoServiceProvider rnd2 = new RNGCryptoServiceProvider(); //may be overdoing it a little
            int rseed = GetNextInt32(rnd2);
            ips = ips.OrderBy(x => rseed).ToList();
            label1.Text = "Seed: " + rseed;
        }
    }
}
