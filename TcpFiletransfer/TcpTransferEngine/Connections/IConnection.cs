using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcpFiletransfer.TcpTransferEngine
{
	internal interface IConnection:IDisposable
	{
		
		bool IsConnected { get;  }
		string RemoteIp { get; set; }
		int Port { get; set; }
		void Connect();
		void DisConnect();
		int Read(byte[] data, int offset, int size);
		bool CanRead { get; }
		bool CanWrite { get; }
		void Write(byte[] data, int offset, int size);
	}
}
