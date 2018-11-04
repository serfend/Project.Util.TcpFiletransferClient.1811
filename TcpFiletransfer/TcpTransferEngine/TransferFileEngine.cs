using File_Transfer.Model.ReceiverFiles;
using File_Transfer.Model.SenderFiles;

using System.Net;

namespace File_Transfer.Model
{
    public class TransferFileEngine : ISender, IReceiver
    {
        private Sender sender;
        private Receiver receiver;

        public TransferFileEngine()
        {
            Sender = new Sender();
            Receiver = new Receiver();
        }


        public string SendIpAdress
        {
            get
            {
                return Sender.SendIpAdress;
            }
            set
            {
                Sender.SendIpAdress = value;
            }
        }

        public IPAddress ReceiveIpAdress
        {
            get
            {
                return Receiver.ReceiveIpAdress;
            }
            set
            {
                Receiver.ReceiveIpAdress = value;
            }
        }

        public int SendPortNumber
        {
            get
            {
                return Sender.SendPortNumber;
            }
            set
            {
                Sender.SendPortNumber = value;
            }
        }

        public int ReceivePortNumber
        {
            get
            {
                return Receiver.ReceivePortNumber;
            }
            set
            {
                Receiver.ReceivePortNumber = value;
            }
        }

        public string SendFileName
        {
            get
            {
                return Sender.SendFileName;
            }
            set
            {
                Sender.SendFileName = value;
            }
        }

        public string ReceiveSaveLocation
        {
            get
            {
                return Receiver.ReceiveSaveLocation;
            }
            set
            {
                Receiver.ReceiveSaveLocation = value;
            }
        }

        public bool IsWaitingForConnect
        {
            get
            {
                return Sender.IsWaitingForConnect;
            }
            set
            {
                Sender.IsWaitingForConnect = value;
            }
        }
        public bool IsSending
        {
            get
            {
                return Sender.IsSending;
            }
            set
            {
                Sender.IsSending = value;
            }
        }

        public bool IsFileReceiving
        {
            get
            {
                return Receiver.IsFileReceiving;
            }
            set
            {
                Receiver.IsFileReceiving = value;
            }
        }
        public bool IsListening
        {
            get
            {
                return Receiver.IsListening;
            }
            set
            {
                Receiver.IsListening = value;
            }
        }

		public Sender Sender { get => sender; set => sender = value; }
		public Receiver Receiver { get => receiver; set => receiver = value; }

		public void ListenForRequest()
        {
            Receiver.ListenForRequest();
        }

        public void SendFile()
        {
            Sender.SendFile();
        }

        public void CancelListeningFile()
        {
            Receiver.CancelListeningFile();
        }

        public void CancelReceivingFile()
        {
            Receiver.CancelReceivingFile();
        }

        public bool CancelSendingFile()
        {
            return Sender.CancelSendingFile();
        }

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Sender.Dispose();
					Receiver.Dispose();
					// TODO: 释放托管状态(托管对象)。
				}

				// TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
				// TODO: 将大型字段设置为 null。

				disposedValue = true;
			}
		}

		// TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
		// ~TransferFileEngine() {
		//   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
		//   Dispose(false);
		// }

		// 添加此代码以正确实现可处置模式。
		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
			// TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
			// GC.SuppressFinalize(this);
		}
		#endregion

	}
}
