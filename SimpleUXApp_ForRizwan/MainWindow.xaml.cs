using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
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

namespace SimpleUXApp_ForRizwan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO written encrypted text is not as expected
        // TODO logical tree error when opening dialogHost
        UserCreds credentials;
        public MainWindow()
        {
            InitializeComponent();
        }

        private string[] SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All files (*)", Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileNames;
            }
            return null;
        }

        private static bool PerformCryption(bool shouldIEncrypt, byte[] key, byte[] iv, string fileName)
        {
            using (AesManaged aesAlg = new AesManaged() { Key = key, IV = iv })
            {
                if (shouldIEncrypt)
                {
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {

                                // Read the file and feed all data to the stream
                                swEncrypt.Write(File.ReadAllBytes(fileName));
                                // Read it back from the memory and write to file
                                File.WriteAllBytes(fileName, msEncrypt.ToArray());
                            }
                        }
                    }
                }
                else
                {
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(File.ReadAllBytes(fileName)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                File.WriteAllBytes(fileName, Encoding.ASCII.GetBytes(srDecrypt.ReadToEnd()));
                            }
                        }
                    }
                }
            }
            return true;
        }

        void CredsDialogClosingHandler(object sender, DialogClosingEventArgs e)
        {
            credentials = new UserCreds(txt_Password.SecurePassword);
            e.Handled = true;
        }

        private void DialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {           
            lbl_Notify.Content = "";

            // Fetch selected files
            string[] selectedFileNames = SelectFile();
            if (selectedFileNames == null)
            {
                lbl_Notify.Content = "Please select a file(s)";
                return;
            }

            // Check which button triggered the event and act accordingly
            bool shouldIEncrypt = ((Button)sender).Content.ToString() == "Encrypt" ? true : false;
            byte[] userKey = GetKey();
            byte[] userIV = GetIV();

            pb_Progress.Maximum = selectedFileNames.Length;
            for (int i = 0; i < selectedFileNames.Length; i++)
            {
                if (PerformCryption(shouldIEncrypt, userKey, userIV, selectedFileNames[i])) // perform operation and inform user
                {
                    lbl_Notify.Content = "File: " + selectedFileNames[i] + " successfully " + (shouldIEncrypt ? "encrypted" : "decrypted");
                }
                else
                {
                    lbl_Notify.Content = "Something unexpected happened with " + selectedFileNames[i] + "! Please try again.";
                }
                pb_Progress.Value = i;
            }

        }

        private byte[] GetKey()
        {
            return new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // stub
        }

        private byte[] GetIV()
        {
            using(AesManaged aes = new AesManaged())
            {
                return aes.IV;
            }
        }
    }
}

public class UserCreds
{
    byte[] Key;
    byte[] IV;

    public UserCreds(SecureString input)
    {

    }
}
