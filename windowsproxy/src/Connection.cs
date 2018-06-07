using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace WindowsProxy
{

    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Text;
    public enum SERVICE_CODES
    {
        COMMAND_LS = 1,
        COMMAND_LS_RESPONSE = 2,
        COMMAND_LS_APPLICATION = 3,
        COMMAND_LS_APPLICATION_RESPONSE = 4,
        COMMAND_UPDATE = 5,
        COMMAND_KEY_PRESS = 7,
        COMMAND_MOUSE_CLICK = 9,
        COMMAND_EVENT = 15,
        COMMAND_QUIT = 100,
    }

    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 8*1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class Connection
    {                
        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.
        public String response = String.Empty;

        private static Socket client;        
        private static Connection connection;

        RemoteProcessUI uiParser; //and set method...
        ListProcessUI listParser;

        public RemoteProcessUI UIParser
        {
            set { uiParser = value; }
        }

        public ListProcessUI ListParser
        {
            set { listParser = value; }
        }

        public static Connection getConnection()
        {
            if (connection != null)
            {
                return connection;
            }

            try
            {
                connection = new Connection();
            }
            catch (Exception e)
            {
                Console.WriteLine("No Connection!! " + e.Message);
                connection = null;
            }
            return connection;
        }
        

        private Connection()
        {                            
            try
            {
                string ip;
                int port;
                using (XmlReader reader = new XmlTextReader("config.xml"))
                {
                    reader.ReadToFollowing("server");
                    reader.MoveToFirstAttribute();
                    ip = reader.Value;                        
                    reader.MoveToNextAttribute();                        
                    port = int.Parse(reader.Value);                        
                }
                
                IPHostEntry ipHostInfo = Dns.Resolve(ip);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                
                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);

                if (!connectDone.WaitOne(5000)) {
                    throw new Exception("Connection Error!!");
                } 
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection Failed!!" + e.Message);                
                throw e;
            }            
        }

        public static void closeConnection()
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();                
                client = null;                
                connection = null;
                Console.WriteLine("Disconnected...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
                Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection CallBack Failed\n");
                Console.WriteLine(e.ToString());                                
            }
        }

        private void Receive()
        {
            try
            {
                // Create the state object.
                if (client == null)
                {
                    Console.WriteLine("Client is null");
                    return;
                }
                StateObject state = new StateObject();
                state.workSocket = client;
                
                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);

                Console.WriteLine("Response received : {0}", response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void call_parser(String response)
        {
            SERVICE_CODES serviceCode;
            using (XmlReader reader = XmlReader.Create(new StringReader(response)))
            {
                reader.ReadToFollowing("service_code");
                reader.MoveToFirstAttribute();
                serviceCode = (SERVICE_CODES)Convert.ToInt64(reader.Value);
            }
            switch (serviceCode)
            {
                case SERVICE_CODES.COMMAND_LS:
                    break;
                case SERVICE_CODES.COMMAND_LS_RESPONSE:
                    listParser.parse(response);
                    break;
                case SERVICE_CODES.COMMAND_LS_APPLICATION:
                    break;
                case SERVICE_CODES.COMMAND_LS_APPLICATION_RESPONSE:
                    uiParser.parse(response);
                    break;
                case SERVICE_CODES.COMMAND_UPDATE:
                    uiParser.parseUpdate(response);
                    break;
                case SERVICE_CODES.COMMAND_KEY_PRESS:
                    break;
                case SERVICE_CODES.COMMAND_MOUSE_CLICK:
                    break;
                case SERVICE_CODES.COMMAND_QUIT:
                    break;
                default:
                    break;
            }   
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            int index = -1;
            char[] temp_buf = new char[8*1024];

            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                //Console.WriteLine(bytesRead);
                if (bytesRead > 0)
                {
                    Console.WriteLine("Before Reading: " + state.sb.Length);

                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    Console.WriteLine("After Reading: " + state.sb.Length);
                    //Console.WriteLine(state.sb.ToString());
                    while ( true )
                    {
                        /* Check if XML Header is present */
                        if (state.sb.ToString().StartsWith(XMLTags.header) ||
                            state.sb.ToString().StartsWith(XMLTags.header2))
                        {
                            Console.WriteLine("Read incoming data(header present): " + state.sb.Length);
                            receiveDone.Set();
                            /* Check if XML trailer is also present */
                            index = state.sb.ToString().IndexOf(XMLTags.trailer);
                            if (index != -1)
                            {
                            
                                /* If the XML ends with the trailer, then the 
                                 * buffer contains exactly 1 valid XML
                                 */
                                if (index + XMLTags.trailer.Length == state.sb.Length)
                                {
                                    Console.WriteLine("Trailer found at end: " + state.sb.Length); 
                                    response = state.sb.ToString();
                                    state.sb.Clear();
                                }
                                else
                                {
                                    /* If there is more XML */
                                    Console.WriteLine("Trailer found in between: " + state.sb.Length);
                                    /* Copy excess XML into temp buffer */
                                    Array.Clear(temp_buf, 0, temp_buf.Length);
                                    state.sb.CopyTo(index + XMLTags.trailer.Length, temp_buf, 0, state.sb.Length-(index+XMLTags.trailer.Length));
                                    /* Remove the excess XML from sb */
                                    state.sb.Remove(index + XMLTags.trailer.Length, state.sb.Length - (index+XMLTags.trailer.Length));
                                    /* Copy complete xml into response */
                                    response = state.sb.ToString();
                                    state.sb.Clear();
                                    Console.WriteLine("After Length" + state.sb.Length);
                                    /* Make sb point to remaining XML */
                                    state.sb.Append(temp_buf, 0, Array.IndexOf<char>(temp_buf, '\0'));
                                }
                                Console.WriteLine(response);
                                call_parser(response);
                                /* If we have consumed complete XML, we exit, else loop around searching for 
                                 * more complete XMLs
                                 */ 
                                if ( state.sb.Length == 0)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                /* If trailer is not present, wait for more data */
                                Console.WriteLine("Trailer not found. Reading more data: "+ state.sb.Length);
                                break;
                            }
                        }
                        else
                        {
                            /* If header is also not present, wait for more data */
                            break;
                        }
                    }

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                        new AsyncCallback(ReceiveCallback), state);

                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {

                        //Console.WriteLine("Inside Second" + state.sb.ToString());
                        //response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void handle_Response(String xml)
        {


        }
        public void Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);


            if (client == null)
            {
                Console.WriteLine("client is null");

                return;
            }
            // Begin sending the data to the remote device.
            try
            {
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);

                sendDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine("PAVAN: SEND FAILED\n");
                Console.WriteLine(e.ToString());
            }

        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}