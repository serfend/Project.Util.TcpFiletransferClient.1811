using System;
using System.Net;

namespace File_Transfer.Model.SenderFiles
{
    interface ISender : IDisposable
    {
		/// <summary>
		/// 此处可为网址
		/// </summary>
        string SendIpAdress { get; set; }


        int SendPortNumber { get; set; }

        string SendFileName { get; set; }

        bool IsWaitingForConnect { get; set; }


        bool IsSending { get;  set; }

        void SendFile();

        bool CancelSendingFile();
    }
}
