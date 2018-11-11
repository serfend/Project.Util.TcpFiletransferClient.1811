using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using File_Transfer;
using File_Transfer.Model.ReceiverFiles;
using static TcpFiletransfer.TcpTransferEngine.Connections.Connection;

namespace TcpFiletransferTest
{
	class Program
	{
		/// <summary>
		/// 使用花生壳预设服务器进行的测试，请替换
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			if (Console.ReadLine() == "send")
			{
				Console.WriteLine("准备发送");

				var engine = new TransferFileEngine(EngineModel.AsServer, "any", 8009);
				var fTestpath = Environment.CurrentDirectory + "//test.txt";

				engine.Sender.ProgressChangedEvent += (x, xx) => {
					Console.WriteLine(xx.ToString());
				};
				engine.Sender.SendingCompletedEvent += (x, xx) =>
				{
					Console.Write("文件传输完成" + xx.Message);
				};
				engine.Connection.ConnectToClient += (x, xx) =>
				{
					if (xx.Result == ReceiveResult.RequestAccepted)
					{

						Console.WriteLine("开始传输文件");
						engine.SendingFile(fTestpath);

					}
					else
					{
						Console.WriteLine("连接失败"+xx.Info);
					}
				};
				engine.Connect();
			}
			else
			{
				Console.WriteLine("等待接收");
				var engine = new TransferFileEngine(EngineModel.AsClient, "1s68948k74.imwork.net", 16397);

				engine.Connection.ConnectedToServer += (x, xx) => {
					if (xx.Success)
					{
						Console.WriteLine("开始接收文件");
						engine.ReceiveFile(Environment.CurrentDirectory + "//recv");
					}
					else
					{
						Console.WriteLine("连接失败:" + xx.Info);
					}
				};
				engine.Receiver.ReceivingCompletedEvent += (x, xx) => {
					Console.WriteLine(xx.Result.ToString());
				};
				engine.Connect();
			}

			while (true)
			{
				Console.WriteLine("等待中");
				Thread.Sleep(1000);
			}
		}
	}
}
