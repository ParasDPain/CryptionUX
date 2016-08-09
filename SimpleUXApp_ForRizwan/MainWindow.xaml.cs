using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace SimpleUXApp_ForRizwan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO written encrypted text is not as expected
        UserCreds credentials;
        bool operationState; // stores shouldIEncrypt state for sharing among parallel methods

        public MainWindow()
        {
            InitializeComponent();
        }

        private string[] SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "All files (*.*)|*.*", Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileNames;
            }
            return null;
        }

        private void CredsDialogClosingHandler(object sender, DialogClosingEventArgs e)
        {
            if (e.Parameter.ToString() != "Cancel") // Ugly patch
            {
                credentials = new UserCreds((e.Parameter as PasswordBox).SecurePassword);
            }
            e.Handled = true;
        }

        private async void btn_Action_Click(object sender, RoutedEventArgs e)
        {
            if (credentials == null)
            {
                var dialogResult = await DialogHost.Show(new PassDialog(), "RootDialog", CredsDialogClosingHandler);
                if (dialogResult.ToString() == "Cancel")
                {
                    return; // exit
                }
            }

            lbl_Notify.Content = "";

            // Store button trigger state
            operationState = ((Button)sender).Content.ToString() == "Encrypt" ? true : false;
            HandleCryption();
        }

        private async void HandleCryption()
        {
            // Fetch selected files
            string[] selectedFileNames = SelectFile();
            if (selectedFileNames == null)
            {
                lbl_Notify.Content = "Please select a file(s)";
                return;
            }

            byte[] userKey = credentials.Key;
            byte[] userIV = credentials.IV;

            // Run encryption on a separate thread
            IProgress<double> progress = new Progress<double>(p => pb_Progress.Value = p);
            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(() =>
            {
                for (int i = 0; i < selectedFileNames.Length; i++)
                {
                    if (PerformCryption(operationState, userKey, userIV, selectedFileNames[i])) // perform operation and inform user
                    {
                        lbl_Notify.Content = "File: " + new FileInfo(selectedFileNames[i]).Name + " successfully " + (operationState ? "encrypted" : "decrypted");
                    }
                    else
                    {
                        lbl_Notify.Content = "Something unexpected happened with " + selectedFileNames[i] + "! Please try again.";
                    }
                    progress.Report((i + 1) * 100 / selectedFileNames.Length); // TODO it updates but still not independently
                }
            }));
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
                    encryptor.Dispose();
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
    }
}

[SecuritySafeCritical]
public class UserCreds
{
    private byte[] key;
    public byte[] Key
    {
        get
        {
            return key;
        }
    }

    private byte[] iv;
    public byte[] IV
    {
        get
        {
            return iv;
        }
    }

    public UserCreds(SecureString input)
    {
        using (AesManaged aes = new AesManaged())
        {
            byte[] salt = Encoding.ASCII.GetBytes("2659b066d1f9e0f928fea83c6f91651c76ca39b9"); // SHA1
            Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(SecureStringToString(input), salt);

            key = pbkdf.GetBytes(aes.KeySize / 8);
            iv = pbkdf.GetBytes(aes.BlockSize / 8);
        }
    }

    // Converts the otherwise leak proof SecureString to a readable format
    // Thanks to http://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string
    private string SecureStringToString(SecureString value)
    {
        IntPtr valuePtr = IntPtr.Zero;
        try
        {
            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
            return Marshal.PtrToStringUni(valuePtr);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }
}
