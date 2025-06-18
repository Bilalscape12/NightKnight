using System;
using System.Windows;

namespace NightKnight
{
    public partial class LocationDialog : Window
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public LocationDialog(double latitude = 40.7128, double longitude = -74.0060)
        {
            InitializeComponent();
            Latitude = latitude;
            Longitude = longitude;
            
            LatitudeTextBox.Text = latitude.ToString();
            LongitudeTextBox.Text = longitude.ToString();
        }

        private void PresetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PresetComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "New York, NY":
                        LatitudeTextBox.Text = "40.7128";
                        LongitudeTextBox.Text = "-74.0060";
                        break;
                    case "Los Angeles, CA":
                        LatitudeTextBox.Text = "34.0522";
                        LongitudeTextBox.Text = "-118.2437";
                        break;
                    case "London, UK":
                        LatitudeTextBox.Text = "51.5074";
                        LongitudeTextBox.Text = "-0.1278";
                        break;
                    case "Tokyo, Japan":
                        LatitudeTextBox.Text = "35.6762";
                        LongitudeTextBox.Text = "139.6503";
                        break;
                    case "Sydney, Australia":
                        LatitudeTextBox.Text = "-33.8688";
                        LongitudeTextBox.Text = "151.2093";
                        break;
                    case "Custom":
                        LatitudeTextBox.Text = "0.0000";
                        LongitudeTextBox.Text = "0.0000";
                        break;
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                Latitude = double.Parse(LatitudeTextBox.Text);
                Longitude = double.Parse(LongitudeTextBox.Text);
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (!double.TryParse(LatitudeTextBox.Text, out double latitude))
            {
                MessageBox.Show("Please enter a valid latitude.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                LatitudeTextBox.Focus();
                return false;
            }

            if (!double.TryParse(LongitudeTextBox.Text, out double longitude))
            {
                MessageBox.Show("Please enter a valid longitude.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                LongitudeTextBox.Focus();
                return false;
            }

            if (latitude < -90 || latitude > 90)
            {
                MessageBox.Show("Latitude must be between -90 and 90 degrees.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                LatitudeTextBox.Focus();
                return false;
            }

            if (longitude < -180 || longitude > 180)
            {
                MessageBox.Show("Longitude must be between -180 and 180 degrees.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                LongitudeTextBox.Focus();
                return false;
            }

            return true;
        }
    }
} 