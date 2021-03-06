﻿using File_Transfer.Model.SenderFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_Transfer.Model.ReceiverFiles
{
	public class ProgressChangedEventArgs : EventArgs
	{
		private long current;
		private long total;
		private long totalTime;

		public ProgressChangedEventArgs(long current, long total, long totalTime)
		{
			this.current = current;
			this.total = total;
			this.totalTime = totalTime;
		}

		public long Current { get => current; set => current = value; }
		public long Total { get => total; set => total = value; }
		public long TotalTime { get => totalTime; set => totalTime = value; }

		public override string ToString()
		{
			return string.Format("{0}/{1}({2})",this.Current,this.Total,this.TotalTime);
		}
	}
	public class SendingCompletedEventArgs : EventArgs
	{
		private SendResult result;
		private string message;
		private string title;

		public SendingCompletedEventArgs(SendResult result, string message, string title)
		{
			this.result = result;
			this.message = message;
			this.title = title;
		}

		public SendResult Result { get => result; set => result = value; }
		public string Message { get => message; set => message = value; }
		public string Title { get => title; set => title = value; }
	}
	public class SendingFileStartedEventArgs : EventArgs{
		private string fileName;
		public SendingFileStartedEventArgs(string fileName)
		{
			this.FileName = fileName;
		}

		public string FileName { get => fileName; set => fileName = value; }
	}
	public class ConnectToServerEventArgs : EventArgs {
		private bool success;
		private string info;

		public ConnectToServerEventArgs(bool success)
		{
			this.success = success;
		}

		public ConnectToServerEventArgs(bool success, string info) : this(success)
		{
			this.info = info;
		}

		public bool Success { get => success; set => success = value; }
		public string Info { get => info; set => info = value; }
	}
	public class ReceivingCompletedEventArgs : EventArgs
	{
		private ReceiveResult result;
		private string message;
		private string title;

		public ReceivingCompletedEventArgs(ReceiveResult result, string message, string title)
		{
			this.result = result;
			this.message = message;
			this.title = title;
		}

		public ReceiveResult Result { get => result; set => result = value; }
		public string Message { get => message; set => message = value; }
		public string Title { get => title; set => title = value; }
	}
	public class ReceivingStartedEventArgs : EventArgs
	{
		private string fileName;
		public ReceivingStartedEventArgs(string fileName)
		{
			this.fileName = fileName;
		}
		public string FileName { get => fileName; set => fileName = value; }
	}
	public class ListenStartedEventArgs : EventArgs
	{

	}
	public class ConnectToClientEventArgs : EventArgs
	{
		private ReceiveResult result;
		private string info;

		public ConnectToClientEventArgs( ReceiveResult result)
		{
			this.Result = result;
		}

		public ConnectToClientEventArgs(ReceiveResult result, string info) : this(result)
		{
			this.info = info;
		}

		public ReceiveResult Result { get => result; set => result = value; }
		public string Info { get => info; set => info = value; }
	}
}
