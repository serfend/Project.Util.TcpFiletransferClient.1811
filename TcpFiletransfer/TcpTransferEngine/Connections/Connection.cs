using File_Transfer.Model.ReceiverFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpFiletransfer.TcpTransferEngine.Connections
{
	public class Connection : IConnection
	{
		public enum EngineModel
		{
			AsServer = 0,
			AsClient = 1
		}
		private readonly EngineModel model;
		private TcpListener TcpListener;
		private NetworkStream NetworkStream;
		private Socket Soket;

		private TcpClient TcpClient;
		#region 服务端
		public delegate void ConnectedToServerEventHandler(Connection sender, ConnectToServerEventArgs e);
		public event ConnectedToServerEventHandler ConnectedToServer;

		#endregion
		#region 客户端
		public delegate void ConnectToClientEventHandler(Connection sender, ConnectToClientEventArgs e);
		public event ConnectToClientEventHandler ConnectToClient;
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ip">当作为服务端时留空即可</param>
		/// <param name="port"></param>
		public Connection(EngineModel model, string ip,int port)
		{
			this.model = model;
			this.RemoteIp = ip;
			this.Port = port;
		}
		public string RemoteIp { get; set; }
		public int Port { get; set; }

		public bool IsConnected { get; private set; }
		public bool IsWaitingForConnect { get; private set; }
		/// <summary>
		/// 取消服务端等待
		/// </summary>
		public bool CancelListening { get; set; }
		public void Connect()
		{
			if (model == EngineModel.AsClient)
			{
				ConnectTo();
			}
			else
			{
				WaitForConnect();
			}
		}
		private void ConnectTo()
		{
			if (!IsConnected)
			{
				try
				{
					IsWaitingForConnect = true;


					TcpClient = new TcpClient(RemoteIp, Port);
					NetworkStream = TcpClient.GetStream();
					IsConnected = true;
					ConnectedToServer?.Invoke(this, new ConnectToServerEventArgs(true));
					
				}
				catch(Exception ex)
				{
					ConnectedToServer?.Invoke(this, new ConnectToServerEventArgs(false,ex.Message));
				}
				finally
				{
					IsWaitingForConnect = false;
				}
			}
			else
			{
				ConnectedToServer?.Invoke(this, new ConnectToServerEventArgs(true, "已处于连接状态"));
			}
		}

		public int Read(byte[] data,int offset,int size) 
		{
			try
			{
				return NetworkStream.Read(data, offset, size);
			}
			catch (System.IO.IOException)
			{
				return -1;
			}
			
		}
		public bool CanRead { get => NetworkStream.CanRead; }
		public bool CanWrite { get => NetworkStream.CanWrite; }
		public void Write(byte[] data,int offset,int size)
		{
			NetworkStream.Write(data, offset, size);
		}


		private void WaitForConnect()
		{
			
			try
			{
				TcpListener = new TcpListener(IPAddress.Any, Port);
				TcpListener.Start();

				bool requestAccepted = false;

				while (!CancelListening && !TcpListener.Pending()) { Thread.Sleep(50); } ;

				if (!CancelListening)
				{
					Soket = TcpListener.AcceptSocket();
					NetworkStream = new NetworkStream(Soket);
					requestAccepted = true;
				}

				if (requestAccepted)
				{
					IsConnected = true;
					ConnectToClient?.Invoke(this,new ConnectToClientEventArgs(ReceiveResult.RequestAccepted));
				}
				else
				{
					ConnectToClient?.Invoke(this, new ConnectToClientEventArgs(ReceiveResult.ListeningCancelled));
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				ConnectToClient?.Invoke(this, new ConnectToClientEventArgs(ReceiveResult.ListeningFailed,ex.Message));
				Dispose();
			}
			
		}

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (NetworkStream != null) NetworkStream.Dispose();
					if (Soket != null) Soket.Dispose();
					if (TcpClient != null) TcpClient.Close();
					if (TcpListener != null) TcpListener.Stop();
					
				}
				NetworkStream = null;
				TcpClient = null;
				Soket = null;
				TcpListener = null;
				disposedValue = true;
			}
		}

		// 添加此代码以正确实现可处置模式。
		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
		}



		#endregion
	}
}
