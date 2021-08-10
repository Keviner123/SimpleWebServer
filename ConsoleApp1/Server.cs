using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class Server
    {
        private int timeout = 8;
        private Encoding charEncoder = Encoding.UTF8; 
        private Socket serverSocket; 
        private string contentPath; 


        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        {
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"}
        };

        public bool start(IPAddress ipAddress, int port, int maxNOfCon, string contentPath)
        {

            //We start our webserver by binding the port, and setting the
            //maximum amount of connections
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ipAddress, port));
            serverSocket.Listen(maxNOfCon);

            //Its very important that we set a reasonable timeout,
            //to miteagate potential hackers filling up our max amount of connections
            serverSocket.ReceiveTimeout = timeout;
            serverSocket.SendTimeout = timeout;
            this.contentPath = contentPath;

            Thread requestListenerT = new Thread(() =>
            {
                while (true)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = serverSocket.Accept();
                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try { handleTheRequest(clientSocket); }
                            catch
                            {
                                try { clientSocket.Close(); } catch { }
                            }
                        });
                        requestHandler.Start();
                    }
                    catch { }
                }
            });
            requestListenerT.Start();

            return true;
        }


        private void handleTheRequest(Socket clientSocket)
        {
            byte[] buffer = new byte[10240]; 
            int receivedBCount = clientSocket.Receive(buffer);
            string strReceived = charEncoder.GetString(buffer, 0, receivedBCount);

            string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

            int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
            int length = strReceived.LastIndexOf("HTTP") - start - 1;
            string requestedUrl = strReceived.Substring(start, length);

            string requestedFile;
            if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
                requestedFile = requestedUrl.Split('?')[0];
            else 
            {
                notImplemented(clientSocket);
                return;
            }

            requestedFile = requestedFile.Replace("/", "\\").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;

            //Display an log message of the request
            Console.WriteLine("User request from: " + clientSocket.RemoteEndPoint + " to " + requestedFile+" using "+httpMethod);


            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                if (extensions.ContainsKey(extension)) 
                    if (File.Exists(contentPath + requestedFile)) 
                        sendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
                    else
                        notFound(clientSocket);
            }
            else
            {
                if (requestedFile.Substring(length - 1, 1) != "\\")
                    requestedFile += "\\";
                if (File.Exists(contentPath + requestedFile + "index.htm"))
                    sendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile + "\\index.htm"), "text/html");
                else if (File.Exists(contentPath + requestedFile + "index.html"))
                    sendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile + "\\index.html"), "text/html");
                else
                    notFound(clientSocket);
            }
        }

        private void notImplemented(Socket clientSocket)
        {
            sendResponse(clientSocket, "Not Implemented", "501 Not Implemented", "text/html");
        }

        private void notFound(Socket clientSocket)
        {
            sendResponse(clientSocket, "File not found", "404 File not found", "text/html");
        }

        private void sendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            sendResponse(clientSocket, bContent, "200 OK", contentType);
        }

        private void sendResponse(Socket clientSocket, string strContent, string responseCode, string contentType)
        {
            byte[] bContent = charEncoder.GetBytes(strContent);
            sendResponse(clientSocket, bContent, responseCode, contentType);
        }

        private void sendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
        {
            try
            {
                //We build a package here using the HTTP standard.
                //https://developer.mozilla.org/en-US/docs/Web/HTTP/Messages
                byte[] bHeader = charEncoder.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Kevins Webserver\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch { }
        }
    }
}
