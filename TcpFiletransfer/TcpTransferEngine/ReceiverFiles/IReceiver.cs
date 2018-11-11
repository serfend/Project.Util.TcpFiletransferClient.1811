using System;
using System.Net;
using TcpFiletransfer.TcpTransferEngine;
using TcpFiletransfer.TcpTransferEngine.Connections;

namespace File_Transfer.Model.ReceiverFiles
{
	interface IReceiver : IDisposable
	{


		string ReceiveSaveLocation { get; set; }

		bool IsFileReceiving { get; set; }

		bool IsListening { get; set; }


		void CancelReceivingFile();

		void CancelListeningFile();

    }
}
