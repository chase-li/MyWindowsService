using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace WindowsService
{
    public class SocketThread : IDisposable
    {
        private Socket socket;
        private Thread thread;
        private bool isListening = false;
        private StringBuilder text = new StringBuilder();
        /// <summary>   
        /// ���췽��   
        /// </summary>   
        /// <param name="socket">���ڴ���ͻ���Ӧ���Socket</param>   
        public SocketThread(Socket socket)
        {
            this.socket = socket;
            isListening = true;
            thread = new Thread(new ThreadStart(Work));
            thread.IsBackground = true;
            thread.Start();
        }


        public void Work()
        {
            ResloveReqs resloveReqs = new ResloveReqs();

            byte[] header = new byte[12];
            string msgHeader = "";
            int msgLength = 0;
            byte[] buffer = new byte[1024];

            byte[] sendByte = new byte[1024];
            string sendMsg;

            try
            {
                int receivedLength = socket.Receive(header);
                //��Ϣͷ���ݼ����ȣ���������Ϣͷ���ȣ�
                msgHeader = System.Text.Encoding.UTF8.GetString(header, 0, receivedLength);
                Log.SetString(msgHeader);
                msgLength = Int32.Parse(msgHeader.Substring(2, 10).Trim());

                while (isListening)
                {
                    receivedLength = socket.Receive(buffer);
                    text.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, receivedLength));

                    msgLength = msgLength - receivedLength;

                    if (msgLength <= 0)
                    {
                        isListening = false;
                        break;
                    }

                }


                if (msgHeader.Substring(0, 2).Equals("01"))
                {//��posid
                    //��ȡ��Ϣ����
                    string posid = text.ToString().Substring(0, 3);
                    if (resloveReqs.GenerateFiles(posid) == 0)
                    {//�ļ��ɹ�����
                        sendMsg = resloveReqs.userFilePath + "|" + resloveReqs.goodFilePath;
                    }
                    else
                    {//�ļ�����ʧ��
                        sendMsg = "1";
                    }
                    Log.SetString(sendMsg);
                    sendByte = Encoding.Default.GetBytes(sendMsg);
                    //�����ļ�·��
                    socket.Send(sendByte);

                }
                else
                {//���ļ�

                    sendMsg = resloveReqs.GetOrderFile(text).ToString();
                    Log.SetString(sendMsg);
                    sendByte = Encoding.Default.GetBytes(sendMsg);
                    socket.Send(sendByte);

                }
            }
            catch (Exception ex)
            {
                Log.SetException(ex);
            }
            finally
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Log.SetString("�ر�socket��");
            }

        }


        #region IDisposable ��Ա

        public void Dispose()
        {
            Log.SetString("����SocketThread Dispose ");
            isListening = false;
            if (thread != null)
            {
                Log.SetString("����SocketThread Dispose " + thread.ThreadState.ToString());
                if (thread.ThreadState != ThreadState.Aborted)
                {
                    thread.Abort();
                }

                Log.SetString("����SocketThread Dispose " + thread.ThreadState.ToString());
                thread = null;
            }
            if (socket != null)
            {
                Log.SetString("����SocketThread Dispose ");
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Log.SetString("����SocketThread Dispose �ͷ�socket");
            }
        }

        #endregion

    }
}
