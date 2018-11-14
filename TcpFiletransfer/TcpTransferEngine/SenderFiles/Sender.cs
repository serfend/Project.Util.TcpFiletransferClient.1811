
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Timers;
using File_Transfer.Model.ReceiverFiles;
using System.ComponentModel;
using TcpFiletransfer.TcpTransferEngine;
using TcpFiletransfer.TcpTransferEngine.Connections;
using System.Collections.Generic;
using System.Threading;

namespace File_Transfer.Model.SenderFiles
{
    public enum SendResult
    {
        Completed,
        CannotSend,
        Cancelled
    }

    public class Sender : ISender
    {
        private const long SEND_BUFFER = 2048;
        private const long INFO_BUFFER = 512;

        private long totalSent = 0;
        private byte[] byteToSend;
		private Thread threadSending;


		
		private Stopwatch ElapsedTime = new Stopwatch();
        private System.Timers.Timer ProgressChangedInvoker = new System.Timers.Timer(500);

        public string SendIpAdress { get; set; }
        public int SendPortNumber { get; set; }
		private List<string> SendingFileQueue=new List<string>();

        private bool IsCancelled { get; set; }
		public delegate void ProgressChanged(object sender, File_Transfer.Model.ReceiverFiles.ProgressChangedEventArgs e);
		public delegate void SendingCompleted(object sender, SendingCompletedEventArgs e);
        public delegate void SendingFileStartedEventHandler(object sender,SendingFileStartedEventArgs e);
		

		
		public event ProgressChanged ProgressChangedEvent;
        public event SendingFileStartedEventHandler SendingFileStartedEvent;
        public event SendingCompleted SendingCompletedEvent;

        public bool IsWaitingForConnect { get; set; } = false;
        public bool IsSending { get; set; }
		private Connection Connection;

		public Sender(ref Connection connection)
        {
			this.Connection = connection;
			threadSending = new Thread(()=> {
				CheckSendingFile();
			});
            Initialize();
        }
		private void CheckSendingFile()
		{
			while (SendingFileQueue.Count > 0)
			{
				var thisFileName = SendingFileQueue[0];
				SendingFileQueue.Remove(thisFileName);
				if (!Connection.IsConnected)
				{
					SendingFileFinished(SendResult.CannotSend, "当前未连接");
					return;
				}
				try
				{
					var returnSendResult = SendResult.Completed;
					string returnMessage = string.Empty;
					string returnMessageTitle = string.Empty;



					byteToSend = GetPacketToSend(thisFileName);
					byte[] buff = new byte[SEND_BUFFER];

					long noOfPack = (byteToSend.Length % SEND_BUFFER == 0) ?
						byteToSend.Length / SEND_BUFFER :
						((byteToSend.Length - (byteToSend.Length % SEND_BUFFER)) / SEND_BUFFER) + 1;

					SendingFileStarted();

					if (Connection != null)
					{
						int loopStep = 0;
						while (noOfPack > 0)
						{
							if (!Connection.CanWrite)
							{
								returnSendResult = SendResult.CannotSend;
								returnMessage = "文件不可写";

								return;
							}
							if (IsCancelled)
							{
								returnSendResult = SendResult.Cancelled;
								returnMessage = "传输被取消";

								return;
							}

							if (noOfPack > 1)
								Array.Copy(byteToSend, loopStep * SEND_BUFFER, buff, 0, SEND_BUFFER);
							else
								Array.Copy(byteToSend, loopStep * SEND_BUFFER, buff, 0, byteToSend.Length % SEND_BUFFER);

							Connection.Write(buff, 0, buff.Length);

							totalSent += buff.Length;

							--noOfPack;
							++loopStep;
						}
						returnSendResult = SendResult.Completed;
						returnMessage = "文件已传输完成";

					}
					else
					{
						returnSendResult = SendResult.CannotSend;
						returnMessage = "传输文件异常";
					}
					SendingFileFinished(returnSendResult, returnMessage, returnMessageTitle);
				}
				catch (ArgumentNullException)
				{
					SendingFileFinished(SendResult.CannotSend, "无参数");
				}
				catch (ArgumentException)
				{
					SendingFileFinished(SendResult.CannotSend, "参数异常");
				}
				catch (PathTooLongException)
				{
					SendingFileFinished(SendResult.CannotSend, "文件路径过长");
				}
				catch (DirectoryNotFoundException)
				{
					SendingFileFinished(SendResult.CannotSend, "未找到路径");
				}
				catch (IOException ex)
				{

					if (ex.InnerException is Win32Exception errcode && errcode.ErrorCode == 10054)
					{
						SendingFileFinished(SendResult.CannotSend, "信道被占用");
					}
					else
					{
						SendingFileFinished(SendResult.CannotSend, "IO异常:" + ex.Message);
					}
				}
				catch (UnauthorizedAccessException)
				{
					SendingFileFinished(SendResult.CannotSend, "无访问权限");
				}
				catch (Exception exception)
				{
					SendingFileFinished(SendResult.CannotSend, exception.Message);
				}
			}
		}
        private void Initialize()
        {
            IsCancelled = false;
            IsSending = false;
            IsWaitingForConnect = false;

            ProgressChangedInvoker.Elapsed += ProgressChangedInvoker_Elapsed;
        }

        private void ProgressChangedInvoker_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (totalSent > SEND_BUFFER * 30 && byteToSend != null && totalSent > 0)
                ProgressChangedEvent?.Invoke(this,new ReceiverFiles.ProgressChangedEventArgs(totalSent, byteToSend.Length, ElapsedTime.ElapsedMilliseconds));
        }

        private void SendingFileStarted()
        {
            IsSending = true;

            ElapsedTime.Reset();
            ElapsedTime.Start();

            ProgressChangedInvoker.Enabled = true;

            SendingFileStartedEvent?.Invoke(this,new SendingFileStartedEventArgs());
        }

        private void SendingFileFinished(SendResult result, string message, string title="传输文件失败")
        {
            IsSending = false;
            ElapsedTime.Stop();

            ProgressChangedInvoker.Enabled = false;
            totalSent = 0;
            byteToSend = null;

            SendingCompletedEvent?.Invoke(this,new SendingCompletedEventArgs(result, message, title));
        }



        private byte[] GetPacketToSend(string SendFileName)
        {
            FileInfo file = new FileInfo(SendFileName);
            string fileInfoStr = file.Length.ToString() + "|" + file.Name + "|";
            byte[] fileInfoByte = Encoding.UTF8.GetBytes(fileInfoStr);

            if (fileInfoByte.Length < (int)INFO_BUFFER)
            {
                int requiredByte = (int)INFO_BUFFER - fileInfoByte.Length;

                for (int i = 0; i < requiredByte; i++)
                {
                    fileInfoStr += " ";
                }
                fileInfoByte = Encoding.UTF8.GetBytes(fileInfoStr);
            }

            byte[] byteToSend;
            byte[] fileByte = File.ReadAllBytes(SendFileName);
            byteToSend = new byte[fileByte.Length + fileInfoByte.Length];

            fileInfoByte.CopyTo(byteToSend, 0);
            fileByte.CopyTo(byteToSend, INFO_BUFFER);

            return byteToSend;
        }


        public bool CancelSendingFile()
        {
            if (IsSending)
            {
                IsCancelled = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public  void SendFile(string  fileName)
		{
			if (SendingFileQueue.Contains(fileName)) return;
			SendingFileQueue.Add(fileName);
			if (IsSending) return;
			threadSending.Start();
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
						Connection.DisConnect();
						Connection.Dispose();
						
					}

					if (ProgressChangedInvoker != null)
					{
						ProgressChangedInvoker.Dispose();
					}
				}

				ProgressChangedInvoker = null;
				disposedValue = true;
			}
		}

		
		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
		}
		#endregion

	}
}
