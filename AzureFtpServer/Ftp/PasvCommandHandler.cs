using System;
using System.Diagnostics;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using AzureFtpServer.Ftp;
using AzureFtpServer.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASV command handler
    /// enter passive mode
    /// </summary>
    internal class PasvCommandHandler : FtpCommandHandler
    {
        private int m_nPort;

        // This command maybe won't work if the ftp server is deployed locally <= firewall
        public PasvCommandHandler(FtpConnectionObject connectionObject)
            : base("PASV", connectionObject)
        {
            // set passive listen port
            m_nPort = int.Parse(ConfigurationManager.AppSettings["FTPPASV"]);
        }

        protected override string OnProcess(string sMessage)
        {
            ConnectionObject.DataConnectionType = DataConnectionType.Passive;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string pasvListenAddress = GetPassiveAddressInfo();

            //return GetMessage(227, string.Format("Entering Passive Mode ({0})", pasvListenAddress));

            // listen at the port by the "FTP" endpoint setting
            int port = int.Parse(ConfigurationManager.AppSettings["FTPPASV"]);
            System.Net.IPAddress ipaddr = SocketHelpers.GetLocalAddress();
            System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(ipaddr.Address, port);

            TcpListener listener = SocketHelpers.CreateTcpListener( ipEndPoint );

            if (listener == null)
            {
                FtpServer.LogWrite(this, sMessage, 550, 0);
                return GetMessage(550, string.Format("Couldn't start listener on port {0}", m_nPort));
            }
            Trace.TraceInformation(string.Format("Entering Passive Mode on {0}", pasvListenAddress));
            SocketHelpers.Send(ConnectionObject.Socket, string.Format("227 Entering Passive Mode ({0})\r\n", pasvListenAddress), ConnectionObject.Encoding);

            listener.Start();

            ConnectionObject.PassiveSocket = listener.AcceptTcpClient();

            listener.Stop();

            sw.Stop();
            FtpServer.LogWrite(this, sMessage, 0, sw.ElapsedMilliseconds);

            return "";
        }

        private string GetPassiveAddressInfo()
        {
            // get routable ipv4 address of load balanced service
            IPAddress ipAddress = SocketHelpers.GetLocalAddress(StorageProviderConfiguration.FtpServerHostPublic);
            if (ipAddress == null)
                throw new Exception("The ftp server do not have a ipv4 address");
            string retIpPort = ipAddress.ToString();
            retIpPort = retIpPort.Replace('.', ',');

            // append the port
            retIpPort += ',';
            retIpPort += (m_nPort / 256).ToString();
            retIpPort += ',';
            retIpPort += (m_nPort % 256).ToString();

            return retIpPort;
        }
    }
}