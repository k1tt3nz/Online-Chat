using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChatClient.ServiceChat;
using ChatHost;
using FeistelNet;

namespace ChatClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IServiceChatCallback
    {
        bool isConnected = false;
        ServiceChat.ServiceChatClient client;
        int ID;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectUser();
        }

        void ConnectUser()
        {
            if (!isConnected)
            {
                client = new ServiceChatClient(new System.ServiceModel.InstanceContext(this));
                try
                {
                    ID = client.Connect(tbUserName.Text);
                    tbUserName.IsEnabled = false;
                    tbIPConn.IsEnabled = false;
                    bConnDiscon.Content = "Disconnect";
                    isConnected = true;
                }
                catch
                {
                    lbChat.Items.Add("Не удалось подключится!");
                }
            }
        }

        void DisconnectUser()
        {
            if (isConnected)
            {
                client.Disconnect(ID);
                client = null;
                tbUserName.IsEnabled = true;
                tbIPConn.IsEnabled = true;
                bConnDiscon.Content = "Connect";
                isConnected = false;
            }
        }

        private bool _isEnteredNickname()
        {
            return tbUserName.Text == string.Empty ? false : true;
        }

        private bool _isEnteredIPConnections()
        {
            return tbIPConn.Text == string.Empty ? false : true;
        }

        private bool _isEnteredLoginDetails()
        {
            return (_isEnteredNickname() & _isEnteredIPConnections()) ? true : false;
        }
        private void bConnect_Click(object sender, RoutedEventArgs e)
        {
            //убрать потом
            if (tbIPConn.Text != "127.0.0.1")
                return;

            if (!_isEnteredLoginDetails() )
                return;

            if (isConnected)
                DisconnectUser();
            else
                ConnectUser();   
        }

        private String _encryptMsg(string msg)
        {
            UInt64[] dMsg = FeistelNetClass.Encrypt(msg);
            String cipherText = Encoding.UTF8.GetString(dMsg.SelectMany(r => BitConverter.GetBytes(r).Reverse()).ToArray());
            return cipherText;
        }

        private string _decryptMsg(string msg)
        {
            String dectryptMsg = FeistelNetClass.Decrypt(msg);

            return dectryptMsg;
        }
        public void MsgCallback(string msg)
        {
            
            lbChat.Items.Add(_decryptMsg(msg));
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]); ;
        }

        private void _sendMsg()
        {
            if (client != null & tbMsg.Text != string.Empty)
            { 
                    string msg = tbMsg.Text;
                    client.SendMsg(msg, ID);
                    tbMsg.Text = string.Empty;
            }
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                _sendMsg();
        }

        private void bSendMsg_Click(object sender, RoutedEventArgs e)
        {
                _sendMsg();
        }

        private void bEmoji_Click(object sender, RoutedEventArgs e)
        {
            if(vbEmojiSet.Visibility == Visibility.Hidden)
                vbEmojiSet.Visibility = Visibility.Visible;
            else
                vbEmojiSet.Visibility = Visibility.Hidden;
        }

        //Блок Emoji

        private void bEmoji_SarcasmFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "🙃";
        }

        private void bEmoji_GrinningFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "😀";
        }

        private void bEmoji_WinkingFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "😉";
        }

        private void bEmoji_SavoringDelicacyFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "😋";
        }

        private void bEmoji_RelaxedFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "😌";
        }

        private void bEmoji_ClownFace_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "🤡";
        }

        private void bEmoji_Poo_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "💩🍺";
        }

        private void bEmoji_Beer_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "🍺🍻";
        }

        private void bEmoji_Beer2_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "🍻";
        }

        private void bEmoji_BabyBottle_Click(object sender, RoutedEventArgs e)
        {
            tbMsg.Text += "🍼";
        }
    }
}
