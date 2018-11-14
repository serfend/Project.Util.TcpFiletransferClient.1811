using System;
using System.Collections.Generic;
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



        bool IsSending { get;  set; }

        void SendFile(string fileName);

        bool CancelSendingFile();
    }
}
