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

namespace Kurs
{
    public partial class ColorDialog : Window
    {
        public Brush FillColor { get { return new SolidColorBrush(fillColor); } }
        public Color fillColor;
        public ColorDialog()
        {
            InitializeComponent();
        }
        public void SetColor(Color clr)
        {
            fillColor = clr;
            Example.Fill = FillColor;
            R.Text = fillColor.R.ToString();
            G.Text = fillColor.G.ToString();
            B.Text = fillColor.B.ToString();
            Hex.Text = fillColor.ToString();
            RS.Value = fillColor.R;
            GS.Value = fillColor.G;
            BS.Value = fillColor.B;
        }
        private void R_LostFocus(Object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(R.Text))
                R.Text = "0";
            if (R.Text.Length > 3)
                return;
            fillColor.R = Byte.Parse(R.Text);
            Example.Fill = FillColor;
        }
        private void G_LostFocus(Object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(G.Text))
                G.Text = "0";
            if (G.Text.Length > 3)
                return;
            fillColor.G = Byte.Parse(G.Text);
            Example.Fill = FillColor;
        }
        private void B_LostFocus(Object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(B.Text))
                B.Text = "0";
            if (B.Text.Length > 3)
                return;
            fillColor.G = Byte.Parse(B.Text);
            Example.Fill = FillColor;
        }
        private void Hex_LostFocus(Object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Hex.Text))
                Hex.Text = "#000";
            if (Hex.Text[0] != '#')
                Hex.Text = "#" + Hex.Text;
            if (Hex.Text.Length != 4 && Hex.Text.Length != 7 && Hex.Text.Length != 9)
                return;
            fillColor = (Color)ColorConverter.ConvertFromString(Hex.Text);
            Example.Fill = FillColor;
        }
        private void All_PreviewTextInput(Object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.Text, 0);
        }
        private void Button_Ok_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
        private void Button_Cancel_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void Slider_R_ValueChanged(Object sender, RoutedPropertyChangedEventArgs<Double> e)
        {
            fillColor.R = (byte)e.NewValue;
            R.Text = fillColor.R.ToString();
            Example.Fill = FillColor;
            Hex.Text = fillColor.ToString();
        }
        private void Slider_G_ValueChanged(Object sender, RoutedPropertyChangedEventArgs<Double> e)
        {
            fillColor.G = (byte)e.NewValue;
            G.Text = fillColor.G.ToString();
            Example.Fill = FillColor;
            Hex.Text = fillColor.ToString();
        }
        private void Slider_B_ValueChanged(Object sender, RoutedPropertyChangedEventArgs<Double> e)
        {
            fillColor.B = (byte)e.NewValue;
            B.Text = fillColor.B.ToString();
            Hex.Text = fillColor.ToString();
            Example.Fill = FillColor;
        }
    }
}
