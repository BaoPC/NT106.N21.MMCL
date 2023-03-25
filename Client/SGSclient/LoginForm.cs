using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SGSclient
{
    
    public partial class LoginForm : Form
    {
        public UdpClient udpClient;
        public string strName;

        public static string strIP { get; set; }
        public UdpClient clientSocket;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            strName = txtName.Text;
            strIP = txtServerIP.Text;
            try
            {
                //Using UDP sockets
                clientSocket = new UdpClient();

                //IP address of the server machine
                IPAddress ipAddress = IPAddress.Parse(txtServerIP.Text);
                //Server is listening on port 10000
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 10000);

                Data msgToSend = new Data();
                msgToSend.cmdCommand = Command.Login;
                msgToSend.strMessage = null;
                msgToSend.strName = strName;

                byte[] byteData = msgToSend.ToByte();

                //Login to the server
                clientSocket.BeginSend(byteData, byteData.Length, ipEndPoint,
                    new AsyncCallback(OnSend), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {                
                strName = txtName.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (txtName.Text.Length > 0 && txtServerIP.Text.Length > 0)
                btnOK.Enabled = true;
            else
                btnOK.Enabled = false;
        }

        private void txtServerIP_TextChanged(object sender, EventArgs e)
        {
            if (txtName.Text.Length > 0 && txtServerIP.Text.Length > 0)
                btnOK.Enabled = true;
            else
                btnOK.Enabled = false;
        }
    }
}
