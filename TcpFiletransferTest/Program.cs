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
		private static void Init()
		{
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
					for (int index = 1; index <= 8; index++)
						engine.SendingFile(string.Format("test ({0}).txt", index));
				}
				else
				{
					Console.WriteLine("连接失败" + xx.Info);
				}
			};



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
					Console.WriteLine("文件接收完成"+xx.Message);
					engine.ReceiveFile(Environment.CurrentDirectory + "//recv");
				}
				else
				{
					Console.WriteLine(xx.Title + ":" + xx.Message);
				}
			};
		}
		private static TransferFileEngine engine ;
		private static void AsSendModel()
		{

		}
		/// <summary>
		/// 使用花生壳预设服务器进行的测试，请替换
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			var port = Convert.ToInt32(Console.ReadLine());
			
			while (Console.ReadLine() != "")
			{
				Console.WriteLine("开始连接");
				if (engine != null) engine.Dispose();
				engine = null;
				if (Console.ReadLine() == "send")
				{
					engine = new TransferFileEngine(EngineModel.AsServer, "any", port);
					Console.WriteLine("准备发送");
				}
				else
				{
					engine = new TransferFileEngine(EngineModel.AsClient, "1s68948k74.imwork.net", port);
					Console.WriteLine("等待接收");
				}
				Init();
				engine.Connect();
			}
		}
	}
}
