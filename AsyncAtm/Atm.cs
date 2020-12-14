using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyLib;

namespace AsyncAtm
{
    public class Atm
    {
        private Socket client;

        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        private Transaction responseTr = new Transaction();

        private void Connect(string dstIp, int dstPort)
        {
            try
            {
                // Punto de conexión con el servidor
                IPAddress ipAddress = System.Net.IPAddress.Parse(dstIp);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, dstPort);
                // Cliente  
                client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Conexión  
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en la conexión.");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                connectDone.Set();
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en la llamada de conexión.");
            }
        }


        private void Send(Transaction tr)
        {
            try
            {
                byte[] byteData = tr.ToByteArray();
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en el envío de datos.");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);
                sendDone.Set();
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en la llamada de envío de datos.");
            }
        }

        private void Receive()
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = client;
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en la recepción de datos.");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                int bytesRead = client.EndReceive(ar);

                // Aunque se lean los datos en una lectura de buffer en el cliente,
                // si no quedan datos por leer se vuelve a llamar a ReceiveCallback,
                // tras esa llamada los bytes leidos serán 0
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        string response = state.sb.ToString();
                        responseTr = Transaction.FromXml(response);
                    }
                    receiveDone.Set();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en la llamada de recepción de datos.");
            }
        }

        public void Disconnect()
        {
            try
            {
                // El servidor lo cierra también tras enviar la respuesta.
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en el cierre del socket del cliente.");
            }
        }

        public void Envio(string srvName, int srvPort, Transaction tr)
        {
            try
            {
                // Inicialización de eventos
                connectDone.Reset();
                sendDone.Reset();
                receiveDone.Reset();
                // Conexión  
                Connect(srvName, srvPort);
                connectDone.WaitOne();
                // Envío  
                Send(tr);
                sendDone.WaitOne();
                // Recepción 
                Receive();
                receiveDone.WaitOne();
                // Salida  
                if (!responseTr.Status)
                {
                    Console.WriteLine("\nLa siguiente transacción no se ha podido llevar a cabo: {0}", responseTr.ToString());
                }
                else
                {
                    Console.Write(".");
                }
                // Cierre 
                Disconnect();
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR en el bloque de envío al servidor.");
            }
        }

        public static int Main(String[] args)
        {
            // Iniciación conexión servidor
            string srvIp = "192.168.1.103";
            int srvPort = 11000;
            // Simula diez clientes
            int N = 10;
            Task[] tasks = new Task[N];
            for (int j = 0; j < N; j++)
            {
                tasks[j] = Task.Run(() =>
                {
                    Atm cliente = new Atm();
                    // Los diez clientes envían diez transacciones cada uno
                    for (int i = 0; i < 10; i++)
                    {
                        Transaction tr = new Transaction("uno", "dos", 100, "");
                        cliente.Envio(srvIp, srvPort, tr);
                    }
                });
            }
            // Esperar a terminar los diez clientes (para ver el resultado de los mensajes mejor)
            for (int j = 0; j < N; j++)
            {
                tasks[j].Wait();
            }
            // Un nuevo cliente que da "problemas", no hay saldo suficiente.
            Task.Run(() =>
            {
                Atm cliente = new Atm();
                // Este envío produce un error si el saldo de "uno" era inicialmente 10000 
                // y ya se había transferido a "dos" esa cantidad
                Transaction trX = new Transaction("uno", "dos", 100, "");
                cliente.Envio(srvIp, srvPort, trX);
                // Este otro es para ver la continuidad del programa y los saldos
                Transaction tr = new Transaction("dos", "uno", 100, "");
                cliente.Envio(srvIp, srvPort, tr);
            });
            // Espera pulsar enter
            Console.Read();
            return 0;
        }
    }
}
