using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DadiUDP_Server
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60000); // Potevo mettere anche 127.0.0.1

            Thread t1 = new Thread(new ParameterizedThreadStart(SocketReceive)); // Thread per la ricezione. // Paramet... da la possibilità di creare un thread con un parametro.
            t1.Start(sourceEndPoint);
        }

        public async void SocketReceive(object sourceEndPoint) // l'async permette di, mentre il thread ascolta di fare le altre cose
        {
            IPEndPoint sourceEP = (IPEndPoint)sourceEndPoint;

            Socket socket = new Socket(sourceEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp); // Dgram perchè usiamo UDP (datagram)
            socket.Bind(sourceEP); // Associa il socket all'indirizzo ip e alla porta.

            byte[] byteRicevuti = new byte[256]; // Posso ricevere solo 256 byte (quindi 256 caratteri).
            int bytes = 0; // Contatore che conta quanti byte ho ricevuto.
            string message = string.Empty;

            // Ciclo che all'infinito controlla se vengono ricevuti bytes, grazie al fatto che ho async il programma non si bloccherà
            await Task.Run(() =>       // Il task è una porzione di thread che andrà ad ascoltare in maniera asincrona il canale( in modo da non bloccare l'interfaccia mentre il Task ascolta).
            {
                while (true)
                {
                    if (socket.Available > 0) // Avaibale è una proprietà che quando è maggiore di 0 indica che il socket ha ricevuto dei dati.
                    {
                        message = string.Empty;
                        bytes = socket.Receive(byteRicevuti, byteRicevuti.Length, 0); // Il primo parametro è l'array su cui verrano caricati i dati, il secondo la sua lunghezza e il terzo una flag che va sempre messa a 0.
                        message += Encoding.ASCII.GetString(byteRicevuti, 0, bytes); // Decodifica l'array di byte in ASCII string.

                        // lblRicezione.Content = message; Darà errore nell'esecuzione perchè per interagire con elementi grafici del WPF bisogna passare per il Dispatcher:

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // lblRicezione.Content = message;
                        }));
                    }
                }
            });
        }

        private void btnLanciaDado_Click(object sender, RoutedEventArgs e)
        {
            bool ipOk = IPAddress.TryParse(txtDestPort.Text, out IPAddress ipDest);
            if (!ipOk)
            {
                txtIpAdd.SelectAll();
                txtIpAdd.Focus();
                MessageBox.Show("Inserire un indirizzo ip valido.");
                return;
            }
            bool portaOk = int.TryParse(txtDestPort.Text, out int portDest);
            if (!portaOk)
            {
                txtDestPort.SelectAll();
                txtDestPort.Focus();
                MessageBox.Show("La porta di destinazione deve essere un numero.");
                return;
            }
            if (portDest > 65535 || portDest < 49152)
            {
                txtDestPort.SelectAll();
                txtDestPort.Focus();
                MessageBox.Show("La porta di destinazione deve essere una private port (range tra 49152 a 65535).");
                return;
            }

            IPEndPoint destinationEndPoint = new IPEndPoint(ipDest, portDest);
            Socket socket = new Socket(ipDest.AddressFamily, SocketType.Dgram, ProtocolType.Udp); // Address family recupera le informazioni sull'indirizzo ip

            Random rdn = new Random();// Inizializza l'oggetto random
            byte[] byteInviati = Encoding.ASCII.GetBytes(rdn.Next(1, 7).ToString());// Decodifica in bytes il numero estratto grazie a rdn.Next().
            socket.SendTo(byteInviati, destinationEndPoint);
        }
    }
}
