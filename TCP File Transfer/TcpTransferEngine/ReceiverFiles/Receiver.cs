
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

		private TcpListener TcpListener;
		private NetworkStream NetworkStream;
		private Socket Soket;

		private Stopwatch ElapsedTime = new Stopwatch();
		private System.Timers.Timer ProgressChangedInvoker = new System.Timers.Timer(500);

		private struct ReceivedFileInfo
		{
			public string FileName { get; set; }
			public long FileSize { get; set; }
		}

		private bool CancelFileReceiving { get; set; }
		private bool CancelListening { get; set; }

		public delegate void ProgressChanged(object sender,ProgressChangedEventArgs e);
		public delegate void ReceivingStarted(object sender, ReceivingStartedEventArgs e);
		public delegate void ReceivingCompleted(object sender, ReceivingCompletedEventArgs e);
		public delegate void ListenStartedEventHandler(object sender, ListenStartedEventArgs e);
		public delegate void ListenCompleted(object sender, ListenCompletedEventArgs e);

		public event ProgressChanged ProgressChangedEvent;
		public event ReceivingStarted ReceivingStartedEvent;
		public event ReceivingCompleted ReceivingCompletedEvent;
		public event ListenCompleted ListenCompletedEvent;
		public event ListenStartedEventHandler ListenStartedEvent;

		public IPAddress ReceiveIpAdress { get; set; }
		public int ReceivePortNumber { get; set; }
		public string ReceiveSaveLocation { get; set; }

		public bool IsFileReceiving { get; set; }
		public bool IsListening { get; set; }

		public Receiver()
		{
			Initialize();
		}

		private void Initialize()
		{
			IsListening = false;
			IsFileReceiving = false;
			CancelFileReceiving = false;
			CancelListening = false;

			ProgressChangedInvoker.Elapsed += ProgressChangedInvoker_Elapsed;
		}

		private void ProgressChangedInvoker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (totalReceived > 0 && fileSize > 0 && IsFileReceiving)
				ProgressChangedEvent.Invoke(this,new ProgressChangedEventArgs(totalReceived, fileSize, ElapsedTime.ElapsedMilliseconds));
		}

		private void ReceivingFileStarted()
		{
			IsFileReceiving = true;
			ElapsedTime.Reset();
			ElapsedTime.Start();

			ProgressChangedInvoker.Enabled = true;

			ReceivingStartedEvent.Invoke(this,new ReceivingStartedEventArgs());
		}

		private void ReceivingFileFinished(ReceiveResult result, string message, string title)
		{
			IsFileReceiving = false;

			ElapsedTime.Stop();

			ProgressChangedInvoker.Enabled = false;

			totalReceived = 0;
			fileSize = 0;

			IsListening = false;

			ReceivingCompletedEvent.Invoke(this,new ReceivingCompletedEventArgs(result, message, title));
		}

		private void ListenStarted()
		{
			IsListening = true;
			TcpListener = new TcpListener(ReceiveIpAdress, ReceivePortNumber);
			TcpListener.Start();

			ListenStartedEvent.Invoke(this,new ListenStartedEventArgs());
		}

		private void ListenFinished(ReceiveResult result)
		{
			ListenCompletedEvent.Invoke(this,new ListenCompletedEventArgs(result));
		}

		public async void ListenForRequest()
		{
			try
			{
				ListenStarted();

				bool requestAccepted = false;
				await Task.Run(async () =>
				{
					while (!CancelListening && !TcpListener.Pending()) ;

					if (!CancelListening)
					{
						Soket = await TcpListener.AcceptSocketAsync();
						NetworkStream = new NetworkStream(Soket);
						requestAccepted = true;
					}
				});
				if (requestAccepted)
				{
					ListenFinished(ReceiveResult.RequestAccepted);
					ReceiveFile();
				}
				else
				{
					ListenFinished(ReceiveResult.ListeningCancelled);
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				ListenFinished(ReceiveResult.ListeningFailed);
			}
		}

		private async void ReceiveFile()
		{
			try
			{
				ReceiveResult returnReceiveResult = ReceiveResult.CannotReceived;
				string returnMessage = string.Empty;
				string returnMessageTitle = string.Empty;

				byte[] fileInfoByte = new byte[INFO_BUFFER];

				await Task.Run(async () =>
				{
					await NetworkStream.ReadAsync(fileInfoByte, 0, (int)INFO_BUFFER);
				});

				ReceivedFileInfo FileInfo = ReadFileInfoFromByte(fileInfoByte);
				fileSize = FileInfo.FileSize;

				using (FileStream fstream = new FileStream(ReceiveSaveLocation + @"\" + FileInfo.FileName, FileMode.Create, FileAccess.ReadWrite))
				{
					byte[] buff = new byte[RECEIVE_BUFFER];
					int buffered = 0;

					ReceivingFileStarted();

					await Task.Run(async () =>
					{
						if (!NetworkStream.CanRead)
						{
							returnReceiveResult = ReceiveResult.CannotReceived;
							return;
						}
						while ((buffered = await NetworkStream.ReadAsync(buff, 0, buff.Length)) > 0)
						{
							if (CancelFileReceiving)
							{
								returnReceiveResult = ReceiveResult.Cancelled;
								return;
							}
							await fstream.WriteAsync(buff, 0, buffered);
							totalReceived += buffered;
						}

						if (totalReceived < FileInfo.FileSize)
						{
							returnReceiveResult = ReceiveResult.CannotReceived;
							return;
						}
						returnReceiveResult = ReceiveResult.Completed;
					});
					ReceivingFileFinished(returnReceiveResult, returnMessage, returnMessageTitle);
				}
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
				ReceivingFileFinished(ReceiveResult.CannotReceived,"未知错误:" +exception.Message,"文件传输失败");
			}
			finally
			{
				Dispose();
			}
		}

		private ReceivedFileInfo ReadFileInfoFromByte(byte[] fileInfoByte)
		{
			string fileInfoStr = Encoding.UTF8.GetString(fileInfoByte);
			int firstPaddingIndex = fileInfoStr.IndexOf('|');
			int lastPaddingIndex = fileInfoStr.LastIndexOf('|');

			long fileSize = long.Parse(fileInfoStr.Substring(0, firstPaddingIndex));
			string fileName = fileInfoStr.Substring(firstPaddingIndex + 1, (lastPaddingIndex - firstPaddingIndex) - 1);

			fileInfoStr = null;

			return new ReceivedFileInfo() { FileName = fileName, FileSize = fileSize };
		}

		public void CancelReceivingFile()
		{
			CancelFileReceiving = true;
		}

		public void CancelListeningFile()
		{
			CancelListening = true;
		}

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

						if (Soket != null)
						{
							Soket.Close();
							Soket.Dispose();
							Soket = null;
						}

						if (TcpListener != null)
						{
							TcpListener.Stop();
							TcpListener = null;
						}

						if (NetworkStream != null)
						{
							NetworkStream.Close();
							NetworkStream.Dispose();
							NetworkStream = null;
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
