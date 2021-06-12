using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ChatLicenta
{
    public partial class Server : Form
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
        TcpClient client;
        Dictionary<string, TcpClient> listaClienti = new Dictionary<string, TcpClient>();
        CancellationTokenSource anulare = new CancellationTokenSource();
        List<string> listaChat = new List<string>();
        public Server()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            anulare = new CancellationTokenSource();
            pornireServer();
        }

        public async  void pornireServer()
        {
            listener.Start();
            scrieInChat("Server started: " + listener.LocalEndpoint);
            scrieInChat("Clients can now connect!");
            try
            {
                int k = 0;
                while (true)
                {
                    k++;
                    client = await Task.Run(() => listener.AcceptTcpClientAsync(), anulare.Token);

                    byte[] nume = new byte[30];
                    NetworkStream stream = client.GetStream();
                    stream.Read(nume, 0, nume.Length);
                    string numeUser = Encoding.ASCII.GetString(nume);
                    numeUser = numeUser.Substring(0, numeUser.IndexOf("$"));

                    listaClienti.Add(numeUser, client);
                    listBox1.Items.Add(numeUser);
                    scrieInChat(numeUser + " has connected " + "@" + client.Client.RemoteEndPoint);
                    anuntare(numeUser + " has Joined", numeUser, false);

                    await Task.Delay(1000).ContinueWith(t => trimiteUseri());
                    var thread = new Thread(() => serverGetData(client, numeUser));
                    thread.Start();
                }
            }
            catch(Exception error)
            {
                listener.Stop();
            }
        }

        public void serverGetData(TcpClient clientTcp, string nume)
        {
            byte[] data = new byte[1024];
            string text = null;

            while (true)
            {
                try
                {
                    NetworkStream stream = clientTcp.GetStream();
                    stream.Read(data, 0, data.Length);
                    List<string> bucati = (List<string>)ByteToObiect(data);

                    switch (bucati[0])
                    {
                        case "chat":
                            this.Invoke((MethodInvoker)delegate
                            {
                                textBox1.Text += nume + ": " + bucati[1] + Environment.NewLine;
                            });
                            anuntare(bucati[1], nume, true);
                            break;
                    }

                    bucati.Clear();
                }
                catch(Exception error)
                {
                    scrieInChat("Client disconenected: " + nume);
                    anuntare("Client disconnected: " + nume + "$", nume, false);
                    listaClienti.Remove(nume);

                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Remove(nume);
                    });
                    trimiteUseri();
                    break;
                }
            }
        }

        public Object ByteToObiect(byte[] arr)
        {
            using(var stream = new MemoryStream())
            {
                var formatare = new BinaryFormatter();
                stream.Write(arr, 0, arr.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var obiect = formatare.Deserialize(stream);
                return obiect;
            }
        }

        public void trimiteUseri()
        {
            try
            {
                byte[] listaUsers = new byte[1024];
                string[] listaClients = listBox1.Items.OfType<string>().ToArray();
                List<string> users = new List<string>();
                users.Add("listaUsers");

                foreach(String nume in listaClients)
                {
                    users.Add(nume);
                }
                listaUsers = ObiectToByte(users);
                
                foreach(var item in listaClienti)
                {
                    TcpClient streamSocket;
                    streamSocket = (TcpClient)item.Value;
                    NetworkStream streamLive = streamSocket.GetStream();
                    streamLive.Write(listaUsers, 0, listaUsers.Length);
                    streamLive.Flush();
                    users.Clear();

                }
                
            }
            catch (SocketException error) { }
        }
        public void anuntare(string mesaj, string numeU, bool switcher)
        {
            try
            {
                foreach(var item in listaClienti)
                {
                    TcpClient socketLive;
                    socketLive = (TcpClient)item.Value;
                    NetworkStream stream = socketLive.GetStream();
                    byte[] streamBytes = null;

                    if (switcher)
                    {
                        listaChat.Add("chat");
                        listaChat.Add(numeU + ": " + mesaj);
                        streamBytes = ObiectToByte(listaChat);
                    }
                    else
                    {
                        listaChat.Add("chat");
                        listaChat.Add(mesaj);
                        streamBytes = ObiectToByte(listaChat);
                    }
                    stream.Write(streamBytes, 0, streamBytes.Length);
                    stream.Flush();
                    listaChat.Clear();
                }
            }
            catch(Exception error)
            {

            }
        }

        public byte[] ObiectToByte(Object obiect)
        {
            BinaryFormatter formatare = new BinaryFormatter();
            using(var stream = new MemoryStream())
            {
                formatare.Serialize(stream, obiect);
                return stream.ToArray();
            }
        }

        public void scrieInChat(String s)
        {
            this.Invoke((MethodInvoker)delegate
            {
                textBox1.AppendText(s + Environment.NewLine);
            });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                listener.Stop();
                scrieInChat("Server has stopped!");
                foreach(var item in listaClienti)
                {
                    TcpClient streamSocket;
                    streamSocket = (TcpClient)item.Value;
                    streamSocket.Close();
                }
            }
            catch(SocketException error) { }
        }
    }
}
