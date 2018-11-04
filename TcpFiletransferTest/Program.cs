using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using File_Transfer;
namespace TcpFiletransferTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var engine = new File_Transfer.Model.TransferFileEngine();
			if (Console.ReadLine() == "send")
			{
				Console.WriteLine("准备发送");
				engine.SendFileName = Environment.CurrentDirectory + "//test.txt";
				engine.SendIpAdress = "1s68948k74.imwork.net";
				engine.SendPortNumber = 16397;
				engine.Sender.ProgressChangedEvent += (x, xx) => {
					Console.WriteLine(xx.ToString());
				};
				engine.Sender.SendingCompletedEvent += (x, xx) =>
				{
					Console.Write("文件传输完成" + xx.Message);
				};

				engine.SendFile();
			}
			else
			{
				Console.WriteLine("等待接收");
				engine.ReceiveIpAdress = IPAddress.Any;
				engine.ReceivePortNumber = 8009;
				engine.ReceiveSaveLocation = Environment.CurrentDirectory + "//recv";
				engine.ListenForRequest();
			}

			while (true)
			{
				Console.WriteLine("等待中");
				Thread.Sleep(1000);
			}
		}
	}
}
