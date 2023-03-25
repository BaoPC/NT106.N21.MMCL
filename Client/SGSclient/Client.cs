using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace SGSclient
{
    //The commands for interaction between the server and the client
    enum Command
    {
        Login,      //dang nhap vao server
        Logout,     //dang xuat
        Message,    //gui tin nhan toi cac client
        List,       //danh sach user trong phong chat tu server
        Null        //No command
    }


    public partial class Client : Form
    {
        public UdpClient clientSocket; //The main client socket
        public string strName ;//Name by which the user logs into the room
        public string strIP = SGSclient.LoginForm.strIP; //IP by which the user logs into the room


        byte []byteData = new byte[1024];

        public Client()
        {
            InitializeComponent();
        }

        //Broadcast the message typed by the user to everyone
        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {		
                //Fill the info for the message to be send
                Data msgToSend = new Data();
                
                msgToSend.strName = strName;
                msgToSend.strMessage = txtMessage.Text;
                msgToSend.cmdCommand = Command.Message;

                byte[] byteData = msgToSend.ToByte();

                //Send it to the server
                clientSocket.Send(byteData, byteData.Length);

                txtMessage.Text = null;
            }
            catch (Exception)
            {
                MessageBox.Show("Khong the gui tin nhan toi server.", "ClientUDP: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }  
        }
        
        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {            
            try
            {
                //IP address of the server machine
                IPAddress ipAddress = IPAddress.Parse(strIP);
                //Server is listening on port 10000
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 10000);


                //clientSocket.Connect(ipEndPoint);
                byteData = clientSocket.EndReceive(ar, ref ipEndPoint);

                //Chuyen sang byte
                Data msgReceived = new Data(byteData);

                //Accordingly process the message received
                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        lstChatters.Items.Add(msgReceived.strName);
                        break;

                    case Command.Logout:
                        lstChatters.Items.Remove(msgReceived.strName);
                        break;

                    case Command.Message:
                        break;

                    case Command.List:
                        lstChatters.Items.AddRange(msgReceived.strMessage.Split('*'));
                        lstChatters.Items.RemoveAt(lstChatters.Items.Count - 1);
                        txtChatBox.Text += "<<<" + strName + " da tham gia phong chat>>>\r\n";
                        break;
                }

                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                    txtChatBox.Text += msgReceived.strMessage + "\r\n";

                byteData = new byte[1024];

                //nhận thêm dữ liệu từ client
                clientSocket.BeginReceive(new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //IP address of the server machine
            IPAddress ipAddress = IPAddress.Parse(strIP);
            //Server is listening on port 10000
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 10000);


            CheckForIllegalCrossThreadCalls = false;

            this.Text = "Client: " + strName;
            
            //client đăng nhập => yêu cầu server lấy tên lên chatroom
            Data msgToSend = new Data ();
            msgToSend.cmdCommand = Command.List;
            msgToSend.strName = strName;
            msgToSend.strMessage = null;

            byteData = msgToSend.ToByte();

            clientSocket.Connect(ipEndPoint);
            clientSocket.Send(byteData, byteData.Length);

            byteData = new byte[1024];
            clientSocket.BeginReceive(new AsyncCallback(OnReceive), null);
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            if (txtMessage.Text.Length == 0)
                btnSend.Enabled = false;
            else
                btnSend.Enabled = true;
        }

        private void SGSClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to leave the chat room?", "Client: " + strName,
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                //Send a message to logout of the server
                Data msgToSend = new Data ();
                msgToSend.cmdCommand = Command.Logout;
                msgToSend.strName = strName;
                msgToSend.strMessage = null;

                byte[] b = msgToSend.ToByte ();
                clientSocket.Send(byteData, byteData.Length);
                clientSocket.Close();
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSend_Click(sender, null);
            }
        }
    }

    //The data structure by which the server and the client interact with each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}