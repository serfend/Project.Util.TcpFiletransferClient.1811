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
				
				var port = Convert.ToInt32(Console.ReadLine());
				var engine = new TransferFileEngine(EngineModel.AsServer, "any", port);

				engine.Sender.ProgressChangedEvent += (x, xx) => {
					Console.WriteLine(xx.ToString());
				};
				engine.Sender.SendingCompletedEvent += (x, xx) =>
				{
					if (xx.Result == File_Transfer.Model.SenderFiles.SendResult.Completed)
					{
						Console.WriteLine("文件传输完成" + xx.Message);
					}
					else
					{
						Console.WriteLine(xx.Title + ":" + xx.Message);
					}
				};
				engine.Connection.ConnectToClient += (x, xx) =>
				{
					if (xx.Result == ReceiveResult.RequestAccepted)
					{

						Console.WriteLine("开始传输文件");
						engine.SendingFile("test.txt");
						engine.SendingFile("test2.txt");
					}
					else
					{
						Console.WriteLine("连接失败"+xx.Info);
					}
				};
				Console.WriteLine("准备发送");
				engine.Connect();
			}
			else
			{
				var port =Convert.ToInt32( Console.ReadLine());
				var engine = new TransferFileEngine(EngineModel.AsClient, "1s68948k74.imwork.net", port);
				int totalFileNum = 2,nowFileNum=0;
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
					if (xx.Result == ReceiveResult.Completed)
					{
						Console.WriteLine("文件接收完成("+ ++nowFileNum +"/" + totalFileNum+")");
						if(nowFileNum <totalFileNum) engine.ReceiveFile(Environment.CurrentDirectory + "//recv");
					}
					else
					{
						Console.WriteLine(xx.Title + ":" + xx.Message);
					}
				};

				Console.WriteLine("等待接收");

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
