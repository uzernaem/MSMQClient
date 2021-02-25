using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Messaging;
using System.Threading;

namespace MSMQ
{
    public partial class frmMain : Form
    {
        private MessageQueue q, r = null;      // очередь сообщений, в которую будет производиться запись сообщений
        Random rnd = new Random();
        private Thread t = null;
        private bool _continue = true;

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            tbLogin.Text += rnd.Next();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (MessageQueue.Exists(tbPath.Text))
            {
                // если очередь, путь к которой указан в поле tbPath существует, то открываем ее
                q = new MessageQueue(tbPath.Text);
                btnSend.Enabled = true;
                btnConnect.Enabled = false;
                tbPath.Enabled = false;
                tbLogin.Enabled = false;

                string path = tbPath.Text.Replace("ServerQueue", tbLogin.Text);    // путь к очереди сообщений, Dns.GetHostName() - метод, возвращающий имя текущей машины

                if (MessageQueue.Exists(path))
                    r = new MessageQueue(path);
                else
                    r = MessageQueue.Create(path);

                // задаем форматтер сообщений в очереди
                r.Formatter = new XmlMessageFormatter(new Type[] { typeof(String) });

                Thread t = new Thread(ReceiveMessage);
                t.Start();
            }
            else
                MessageBox.Show("Указан неверный путь к очереди, либо очередь не существует");
        }

        private void ReceiveMessage()
        {
            if (r == null)
                return;
            System.Messaging.Message msg = null;
            // входим в бесконечный цикл работы с очередью сообщений
            while (_continue)
            {
                if (r.Peek() != null)   // если в очереди есть сообщение, выполняем его чтение, интервал до следующей попытки чтения равен 10 секундам
                    msg = r.Receive(TimeSpan.FromSeconds(10.0));
                rtbMessages.Invoke((MethodInvoker)delegate
                {
                    if (msg != null)
                        rtbMessages.Text += "\n >> " + msg.Label + " : " + msg.Body;     // выводим полученное сообщение на форму
                });
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (q != null)
            {
                q.Send("_logout", tbLogin.Text);
            }
            if (t != null)
            {
                t.Abort();          // завершаем поток
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // выполняем отправку сообщения в очередь
            if (tbMessage.Text == "_logout")
                tbMessage.Text = "logout";
            q.Send(tbMessage.Text, tbLogin.Text);
            tbMessage.Text = "";
        }
    }
}