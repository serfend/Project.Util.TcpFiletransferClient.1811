﻿
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpFiletransfer.TcpTransferEngine;
using TcpFiletransfer.TcpTransferEngine.Connections;

namespace File_Transfer.Model.ReceiverFiles
{
	public enum ReceiveResult
	{
		Completed,
		CannotReceived,
		Cancelled,
		FileIgnoredByUser,
		ListeningCancelled,
		ListeningFailed,
		RequestAccepted
	}

	public class Receiver : IReceiver
	{
		private const long RECEIVE_BUFFER = 2048;
		private const long INFO_BUFFER = 512;

		private long totalReceived = 0;
		private long fileSize = 0;



		private Stopwatch ElapsedTime = new Stopwatch();
		private System.Timers.Timer ProgressChangedInvoker = new System.Timers.Timer(500);

		private struct ReceivedFileInfo
		{
			public string FileName { get; set; }
			public long FileSize { get; set; }
		}

		private bool CancelFileReceiving { get; set; }

		public delegate void ProgressChanged(Receiver sender,ProgressChangedEventArgs e);
		public delegate void ReceivingStarted(Receiver sender, ReceivingStartedEventArgs e);
		public delegate void ReceivingCompleted(Receiver sender, ReceivingCompletedEventArgs e);

		public event ProgressChanged ProgressChangedEvent;
		public event ReceivingStarted ReceivingStartedEvent;
		public event ReceivingCompleted ReceivingCompletedEvent;



		public string ReceiveSaveLocation { get; set; }

		public bool IsFileReceiving { get; set; }
		public bool IsListening { get; set; }

		public Connection Connection;
		public Receiver(ref Connection connection)
		{
			this.Connection = connection;
			ProgressChangedInvoker.Elapsed += ProgressChangedInvoker_Elapsed;
		}


		private void ProgressChangedInvoker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (totalReceived > 0 && fileSize > 0 && IsFileReceiving)
				ProgressChangedEvent?.Invoke(this,new ProgressChangedEventArgs(totalReceived, fileSize, ElapsedTime.ElapsedMilliseconds));
		}

		private void ReceivingFileStarted(string fileName)
		{
			IsFileReceiving = true;
			ElapsedTime.Reset();
			ElapsedTime.Start();

			ProgressChangedInvoker.Enabled = true;

			ReceivingStartedEvent?.Invoke(this,new ReceivingStartedEventArgs(fileName));
		}

		private void ReceivingFileFinished(ReceiveResult result, string message, string title)
		{
			IsFileReceiving = false;
			
			ElapsedTime.Stop();

			ProgressChangedInvoker.Enabled = false;

			totalReceived = 0;
			fileSize = 0;

			IsListening = false;

			ReceivingCompletedEvent?.Invoke(this,new ReceivingCompletedEventArgs(result, message, title));
		}



		public  void ReceiveFile()
		{
			try
			{
				ReceivedFileInfo FileInfo=new ReceivedFileInfo();

				byte[] fileInfoByte = new byte[INFO_BUFFER];
				int firstTimeIn = 0, infoEndPoint=0,tryTime=0;
				bool mutiReceiveFlag = false;
				while (true)
				{
					Connection.Read(fileInfoByte, firstTimeIn, (int)(INFO_BUFFER-firstTimeIn));

					string fileInfoStr = Encoding.UTF8.GetString(fileInfoByte);
					//Console.WriteLine("receiveinfo:" + fileInfoStr);
					FileInfo = ReadFileInfoFromByte(fileInfoByte, out  infoEndPoint);
					fileSize = FileInfo.FileSize;

					for (int i = infoEndPoint + 1; i < INFO_BUFFER; i++)
					{
						if (fileInfoByte[i] == 0) break;
						firstTimeIn++;
					}
					if((fileSize <= 0 && firstTimeIn > INFO_BUFFER))
					{
						ReceivingFileFinished(ReceiveResult.CannotReceived, "文件标头解析失败", "连接失败");
						return;
					}
					else if (fileSize <=0|| tryTime++>3)
					{
						ReceivingFileFinished(ReceiveResult.CannotReceived, "文件长度为0", "连接失败");
						return;
					}
					else if (fileSize > 0) break;
					firstTimeIn++;
					mutiReceiveFlag = true;
				}
				if (mutiReceiveFlag)
				{
					firstTimeIn = 0;
					for (int i = infoEndPoint + 1; i < INFO_BUFFER; i++)
					{
						if (fileInfoByte[i] == 0) break;
						firstTimeIn++;
					}
				}
				
				using (FileStream fstream = new FileStream(ReceiveSaveLocation + @"\" + FileInfo.FileName, FileMode.Create, FileAccess.ReadWrite))
				{
					byte[] buff = new byte[RECEIVE_BUFFER];
					int buffered = 0;

					ReceivingFileStarted(FileInfo.FileName);


					if (!Connection.CanRead)
					{
						ReceivingFileFinished(ReceiveResult.CannotReceived, "连接无法读取", "连接失效");
						return;
					}
					
					fstream.Write(fileInfoByte, infoEndPoint + 1, firstTimeIn);
					totalReceived += firstTimeIn;
					int thisTimeBuffer = FileInfo.FileSize - totalReceived > buff.Length ? buff.Length : (int)(FileInfo.FileSize - totalReceived);
					if(thisTimeBuffer>0)
					while ((buffered = Connection.Read(buff, 0, thisTimeBuffer)) > 0)
					{
						if (CancelFileReceiving)
						{
							ReceivingFileFinished(ReceiveResult.Cancelled, "文件接收被取消", "连接失效");
							return;
						}
						fstream.Write(buff, 0, buffered);
						totalReceived += buffered;
						if (totalReceived >= FileInfo.FileSize) break;
					}
					if (totalReceived > FileInfo.FileSize)
					{
						Console.WriteLine("超出预期数据量");
					}
					if (totalReceived < FileInfo.FileSize)
					{
						ReceivingFileFinished(ReceiveResult.CannotReceived, "接收到的数据不完整", "连接错误");
						return;
					}
					
				}
				//准备开始下次传输的等待
				var data = Encoding.UTF8.GetBytes("#####");
				Connection.Write(data, 0, 5);
				totalReceived = 0;
				ReceivingFileFinished(ReceiveResult.Completed, "file:" + FileInfo.FileName + ",size:" + FileInfo.FileSize, "接收完成");
			}
			catch (IOException ex)
			{

				if (ex.InnerException is Win32Exception errcode && errcode.ErrorCode == 10054)
				{
					ReceivingFileFinished(ReceiveResult.CannotReceived, "当前存在其他用户", "文件操作失败");
				}
				else
				{
					ReceivingFileFinished(ReceiveResult.CannotReceived, "IO错误", "文件操作失败");
				}
			}
			catch (Exception exception)
			{
				ReceivingFileFinished(ReceiveResult.CannotReceived, "未知错误:" + exception.Message, "文件传输失败");
			}
		}
		private ReceivedFileInfo ReadFileInfoFromByte(byte[] fileInfoByte,out int infoEndPoint)
		{
			infoEndPoint = 0;
			try
			{
				string fileInfoStr = Encoding.UTF8.GetString(fileInfoByte);
				int firstPaddingIndex = fileInfoStr.IndexOf('|');
				int secondPaddingIndex = fileInfoStr.IndexOf('|',firstPaddingIndex+1);
				long fileSize = long.Parse(fileInfoStr.Substring(0, firstPaddingIndex));
				string fileName = fileInfoStr.Substring(firstPaddingIndex + 1, (secondPaddingIndex - firstPaddingIndex) - 1);
				var FileInfoSeq = fileInfoStr.Substring(0, secondPaddingIndex);
				infoEndPoint = Encoding.UTF8.GetBytes(FileInfoSeq).Length;
				return new ReceivedFileInfo() { FileName = fileName, FileSize = fileSize };
			}
			catch (Exception)
			{
				return new ReceivedFileInfo();
			}
			
		}

		public void CancelReceivingFile()
		{
			CancelFileReceiving = true;
		}

		public void CancelListeningFile()
		{
			Connection.CancelListening=true;
		}

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

					
					if (Connection != null)
					{
						Connection.Dispose();
						
					}
					if (ProgressChangedInvoker != null)
					{
						ProgressChangedInvoker.Dispose();
						ProgressChangedInvoker = null;
					}
				}


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
