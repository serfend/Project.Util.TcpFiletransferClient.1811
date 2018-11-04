using System;
using System.Net;

namespace File_Transfer.Model.ReceiverFiles
{
    interface IReceiver : IDisposable
    {

        IPAddress ReceiveIpAdress { get; set; }

        int ReceivePortNumber { get; set; }

        string ReceiveSaveLocation { get; set; }

        bool IsFileReceiving { get; set; }

        bool IsListening { get; set; }

        void ListenForRequest();

        void CancelReceivingFile();

        void CancelListeningFile();
    }
}
