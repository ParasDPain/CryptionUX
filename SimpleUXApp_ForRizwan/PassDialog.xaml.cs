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

namespace SimpleUXApp_ForRizwan
{
    /// <summary>
    /// Interaction logic for PassDialog.xaml
    /// </summary>
    public partial class PassDialog : UserControl
    {
        public PassDialog()
        {
            InitializeComponent();
        }

        // Only allow click if field is not empty
        private void txt_Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            btn_DialogSubmit.IsEnabled = txt_Password.SecurePassword.Length > 0 ? true : false;
        }
    }
}
