using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ScreenCast
{
    public partial class Main : Form
    {
        private TcpListener listener;
        int port = 8090;
        public Main()
        {
            InitializeComponent();
        }
        private void Start()
        {
            // Create a TCP listener on the specified port
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            // Start accepting clients asynchronously
            Task.Run(() => AcceptClients());
        }

        private void AcceptClients()
        {
            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Task.Run(() => SendScreenFrames(client));
                }
                catch
                {
                }
            }
        }


        private void SendScreenFrames(TcpClient client)
        {
            const string Boundary = "MyFrame";// CHNAGE ITO PAG MULTIPLE SCREEN
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    string initialBoundary = "" +
                      $"HTTP/1.1 200 OK\r\n" +
                      $"Content-Type: multipart/x-mixed-replace; boundary=--{Boundary}\r\n\r\n";

                    byte[] initialBoundaryBytes = Encoding.ASCII.GetBytes(initialBoundary);
                    stream.Write(initialBoundaryBytes, 0, initialBoundaryBytes.Length);

                    while (client.Connected)
                    {
                        CaptureScreen(stream, Boundary);

                    }
                }
                catch { }
            }
        }

        private void CaptureScreen(NetworkStream stream, string boundary)
        {
            try
            {

                //KINIKUHA MUNA NATIN YUNG PERCENTAGE NG SCREEN SETTING NG WINDOWS PARA TAMA BUO ANG MAKUHA SA SCREEN
                double factor = ScreenPercentage.scale();
                int width = ((int)(Screen.PrimaryScreen.Bounds.Width) * (int)(factor)) / 100;
                int height = ((int)(Screen.PrimaryScreen.Bounds.Height) * (int)(factor) / 100);


                //ILALAGAY NANATIN SA BITMAP YUNG SIZE NG IMAGE
                using (var bmpScreenCapture = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(bmpScreenCapture))
                {
                    int boundX = Screen.PrimaryScreen.Bounds.X;
                    int boundY = Screen.PrimaryScreen.Bounds.Y;
                    graphics.CopyFromScreen(boundX, boundY, 0, 0, bmpScreenCapture.Size, CopyPixelOperation.SourceCopy);

                    //I SEND NA NATIN SA BROWSER
                    using (var ms = new MemoryStream())
                    {
                        bmpScreenCapture.Save(ms, ImageFormat.Jpeg);
                        string header = $"--{boundary}\r\n" +
                                        $"Content-Type: image/jpeg\r\n" +
                                        $"Content-Length: {ms.Length}\r\n\r\n";

                        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                        stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                        ms.Position = 0;
                        ms.CopyTo(stream);
                        stream.FlushAsync();
                    }
                }
            }
            catch { }
        }

        private void UpdateHTML()
        {
            //UPDATE HTML PARA SA DYNAMIC IP ADDRESS
            string Ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            string path = Environment.CurrentDirectory.Replace("\\bin\\Debug", "") + "\\index.html";
            string ImagePath = "http://" + Ip + ":" + port.ToString();
            string image = "<img src=\"" + ImagePath + "\" style=\"\r\n        height: 100%;\r\n        bottom: 0;\r\n        left: 0;\r\n        margin: auto;\r\n        overflow: auto;\r\n        right: 0;\r\n        top: 0px;\r\n        -o-object-fit: contain;\r\n        object-fit: contain;\r\n        border: none;\r\n        border-width: 0px;\r\n        border-color: white;\r\n        -webkit-user-drag: none;\r\n        user-drag: none;\r\n        user-select: none;\r\n        pointer-events: none;\r\n        position:fixed;\"/>";
            File.WriteAllText(path, image);
            try
            {
                Task.Delay(1000);
                Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Main_Load(object sender, EventArgs e)
        {
            Start(); 
            UpdateHTML();
        }
    }
}
