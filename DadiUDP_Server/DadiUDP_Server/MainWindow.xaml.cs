/*
* Autore: Pisu Roberto
* Classe: 4^L
* Data inizio: 2021/05/11
* Scopo: Realizzare una simulazione del lancio di un dado contro un avversario tramite il protocollo UDP.
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DadiUDP_Server
{
    /// <summary>
    /// Server (H2)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60000); // Potevo mettere anche 127.0.0.1

            Thread t1 = new Thread(new ParameterizedThreadStart(SocketReceive)); // Thread per la ricezione. ParameterizedThreadStart da la possibilità di creare un thread con un parametro.
            t1.Start(sourceEndPoint);

            // Messi per velocizzare le fasi di testing:
            txtDestPort.Text = "60001";
            txtIpAdd.Text = "127.0.0.1";
        }

        private string message;
        public async void SocketReceive(object sourceEndPoint) // l'async permette di, mentre il thread ascolta di fare le altre cose
        {
            IPEndPoint sourceEP = (IPEndPoint)sourceEndPoint;

            Socket socket = new Socket(sourceEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp); // Dgram perchè usiamo UDP (datagram)
            socket.Bind(sourceEP); // Associa il socket all'indirizzo ip e alla porta.

            byte[] byteRicevuti = new byte[256]; // Posso ricevere solo 256 byte (quindi 256 caratteri).
            int bytes = 0; // Contatore che conta quanti byte ho ricevuto.
            message = string.Empty;

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
                        MostraRisultato();
                        SceltaImmagine(message, false);
                    }
                }
            });
        }

        private int numeroEstratto;

        private void btnLanciaDado_Click(object sender, RoutedEventArgs e)
        {
            if (txtIpAdd.Text == "")
            {
                txtIpAdd.SelectAll();
                txtIpAdd.Focus();
                MessageBox.Show("Inserire l'ip di destinazione.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (txtDestPort.Text == "")
            {
                txtDestPort.SelectAll();
                txtDestPort.Focus();
                MessageBox.Show("Inserire la porta di destinazione.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool ipOk = IPAddress.TryParse(txtIpAdd.Text, out IPAddress ipDest);
            if (!ipOk)
            {
                txtIpAdd.SelectAll();
                txtIpAdd.Focus();
                MessageBox.Show("Inserire un indirizzo ip valido.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool portaOk = int.TryParse(txtDestPort.Text, out int portDest);
            if (!portaOk)
            {
                txtDestPort.SelectAll();
                txtDestPort.Focus();
                MessageBox.Show("La porta di destinazione deve essere un numero.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (portDest > 65535 || portDest < 49152)
            {
                txtDestPort.SelectAll();
                txtDestPort.Focus();
                MessageBox.Show("La porta di destinazione deve essere una private port (range tra 49152 a 65535).", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IPEndPoint destinationEndPoint = new IPEndPoint(ipDest, portDest);
            Socket socket = new Socket(ipDest.AddressFamily, SocketType.Dgram, ProtocolType.Udp); // Address family recupera le informazioni sull'indirizzo ip

            Random rdn = new Random();// Inizializza l'oggetto random
            numeroEstratto = rdn.Next(1, 7);
            MostraRisultato();
            SceltaImmagine(numeroEstratto.ToString(), true);

            byte[] byteInviati = Encoding.ASCII.GetBytes(numeroEstratto.ToString());// Decodifica in bytes il numero estratto.
            socket.SendTo(byteInviati, destinationEndPoint);
        }

        /// <summary>
        /// Metodo che mostra il risultato dei dadi (Specifica se ho vinto, se ho perso o se è un pareggio).
        /// </summary>
        private void MostraRisultato()
        {
            if (numeroEstratto != 0 && message != "")
            {
                object coloreTesto, coloreSfondo;
                string messaggio = string.Empty;
                if (numeroEstratto > int.Parse(message))
                {
                    coloreTesto = Brushes.Green;
                    coloreSfondo = Brushes.LightGreen;
                    message = "Hai vinto!";
                }
                else if (numeroEstratto < int.Parse(message))
                {
                    coloreTesto = Brushes.DarkRed;
                    coloreSfondo = Brushes.Red;
                    message = "Hai perso!";
                }
                else
                {
                    coloreTesto = Brushes.DarkBlue;
                    coloreSfondo = Brushes.LightBlue;
                    message = "Pareggio!";
                }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    lblRisultato.Foreground = coloreTesto as Brush;
                    lblRisultato.Background = coloreSfondo as Brush;
                    lblRisultato.Content = message;
                }));
            }
        }

        /// <summary>
        /// Metodo per la scelta di quale immagine mostrare e dove mostrarla.
        /// </summary>
        /// <param name="indiceImmagine">Contiene la stringa che i due giocatori si scambiano, che indica il numero uscito dal lancio del dado.</param>
        /// <param name="f">Flag che se impostata a false indica che l'immagine del dado da mostrare appartiene all'avversario.</param>
        private void SceltaImmagine(string indirizzamentoImmagine, bool f)
        {
            switch (indirizzamentoImmagine)
            {
                case "1":
                    MostraImmagine("one", f);
                    break;
                case "2":
                    MostraImmagine("two", f);
                    break;
                case "3":
                    MostraImmagine("three", f);
                    break;
                case "4":
                    MostraImmagine("four", f);
                    break;
                case "5":
                    MostraImmagine("five", f);
                    break;
                case "6":
                    MostraImmagine("six", f);
                    break;
            }
        }

        /// <summary>
        /// Metodo che mostra l'immagine nell'apposita griglia.
        /// </summary>
        /// <param name="nomeImmagine">Stringa contenente il nome dell'immagine (contenuta nella cartella FaceDice) da mostrare</param>
        /// <param name="flag">Se true indica che l'immagine è da inserire nella griglia associata la mio dado, altrimenti nella griglia associata al dado avversario.</param>
        private void MostraImmagine(string nomeImmagine, bool flag)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Image image = new Image
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 197,
                    Height = 179,
                    Margin = new Thickness(10, 10, 0, 0),
                };
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"/FaceDice/" + nomeImmagine + ".jpg", UriKind.Relative);
                bitmap.EndInit();
                image.Source = bitmap;
                if (flag)
                    grdDadoMio.Children.Add(image);
                else
                    grdDadoAvversario.Children.Add(image);
            }));
        }
    }
}
