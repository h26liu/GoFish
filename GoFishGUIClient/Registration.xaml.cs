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
using System.Windows.Shapes;

namespace GoFishGUIClient
{
    /// <summary>
    /// Interaction logic for Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void startgameBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(tbPlayerName.Text))
            {
                MainWindow mainWindow = new MainWindow(tbPlayerName.Text);

                if (mainWindow.callbackID != -1)
                    mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a name to play!");
            }
        }
    }
}
