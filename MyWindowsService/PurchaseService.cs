using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace WindowsService
{
    public partial class MyPurchaseService : ServiceBase
    {
        static Socket socketwatch = null;

        public MyPurchaseService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string serverIP = ConfigurationManager.AppSettings["serverIP"].ToString().Trim();
            string serverPort = ConfigurationManager.AppSettings["serverPort"].ToString().Trim();
            int Port = int.Parse(serverPort);
            //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）  
            socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //服务端发送信息需要一个IP地址和端口号  
            IPAddress address = IPAddress.Any;
            //IPAddress address = IPAddress.Parse(serverIP);
            //将IP地址和端口号绑定到网络节点point上  
            IPEndPoint point = new IPEndPoint(address, Port);

            //监听绑定的网络节点  
            socketwatch.Bind(point);

            //将套接字的监听队列长度限制为100  
            socketwatch.Listen(100);

            //负责监听客户端的线程:创建一个监听线程  
            Thread threadwatch = new Thread(watchconnecting);

            //将窗体线程设置为与后台同步，随着主线程结束而结束  
            threadwatch.IsBackground = true;

            //启动线程     
            threadwatch.Start();

            Log.writelog("开启监听。。。");
        }

        //监听客户端发来的请求  
        static void watchconnecting()
        {
            Socket connection = null;

            //持续不断监听客户端发来的请求     
            while (true)
            {
                try
                {
                    connection = socketwatch.Accept();
                }
                catch (Exception ex)
                {
                    //提示套接字监听异常     
                    Log.writelog(ex.Message);
                    break;
                }

                //获取客户端的IP和端口号  
                IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;
                int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;

                //让客户显示"连接成功的"的信息  
                string sendmsg = "连接服务端成功！\r\n" + "本地IP:" + clientIP + "，本地端口" + clientPort.ToString();
                Log.writelog(sendmsg);

                //客户端网络结点号  
                string remoteEndPoint = connection.RemoteEndPoint.ToString();
                //显示与客户端连接情况
                Log.writelog("成功与" + remoteEndPoint + "客户端建立连接！\t\n");

                SocketThread thread = new SocketThread(connection);
            }
        }

        protected override void OnStop()
        {
            Log.writelog("退出监听，并关闭程序。");
        }
    }
}
