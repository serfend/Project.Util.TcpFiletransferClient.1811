using File_Transfer.Model.ReceiverFiles;
using File_Transfer.Model.SenderFiles;
using System;
using System.Net;
using TcpFiletransfer.TcpTransferEngine.Connections;
using static TcpFiletransfer.TcpTransferEngine.Connections.Connection;

namespace File_Transfer
{
    public class TransferFileEngine:IDisposable 
    {
		private Sender sender;
		private Receiver receiver;
		public Connection Connection;

		public Sender Sender { get => sender; private set=>sender=value; }
		public Receiver Receiver { get => receiver; private set => receiver = value; }

		
		public TransferFileEngine(EngineModel model,string ip,int port) {
			Connection = new Connection(model,ip, port);
			sender = new Sender(ref Connection);
			receiver = new Receiver(ref Connection);
		}
		public void Disconnect()
		{
			Connection.DisConnect();
		}
		public void Connect() => Connection.Connect();
		public void SendingFile(string fileName)
		{
			Sender.SendFileName = fileName;
			sender.SendFile();
		}
		public void CancelSendingFile() { Sender.CancelSendingFile(); }
		public void ReceiveFile(string savePath)
		{
			Receiver.ReceiveSaveLocation = savePath;
			Receiver.ReceiveFile();
		}
		public void CancelReceiveFile() { Receiver.CancelReceivingFile(); }

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Connection.DisConnect();
					Connection.Dispose();
					if (sender != null) sender.Dispose();
					if (receiver != null) receiver.Dispose();
				}
				Connection = null;
				Sender = null;
				Receiver = null;
				disposedValue = true;
			}
		}

		// 添加此代码以正确实现可处置模式。
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
