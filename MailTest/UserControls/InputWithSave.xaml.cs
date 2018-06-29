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

namespace MailTest
{
    /// <summary>
    /// Interaction logic for InputWithSave.xaml
    /// </summary>
    public partial class InputWithSave : UserControl
    {
        public string Label
        {
            get => (string)label.Content;
            set => label.Content = value;
        }

        public string Text
        {
            get => textBox.Text;
            set => textBox.Text = value;
        }

        public bool Save
        {
            get => save.IsChecked == true;
            set => save.IsChecked = value;
        }

        public bool SaveVisible
        {
            get => save.IsVisible;
            set => save.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public double TextBoxHeight
        {
            set => textBox.Height = value;
            get => textBox.Height;
        }

        public string Watermark { get; set; }

        public InputWithSave()
        {
            InitializeComponent();
        }
    }
}
