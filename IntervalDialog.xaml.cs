using System;
using System.Windows;

namespace NightKnight
{
    public partial class IntervalDialog : Window
    {
        public FilterInterval? Result { get; private set; }

        public IntervalDialog(FilterInterval? interval = null)
        {
            InitializeComponent();
            InitializeEventHandlers();
            
            if (interval != null)
            {
                LoadInterval(interval);
            }
        }

        private void InitializeEventHandlers()
        {
            GreenReductionSlider.ValueChanged += (s, e) =>
            {
                GreenReductionText.Text = $"{GreenReductionSlider.Value:P0}";
            };

            BlueReductionSlider.ValueChanged += (s, e) =>
            {
                BlueReductionText.Text = $"{BlueReductionSlider.Value:P0}";
            };

            GradualStartCheckBox.Checked += (s, e) => StartTransitionGrid.Visibility = Visibility.Visible;
            GradualStartCheckBox.Unchecked += (s, e) => StartTransitionGrid.Visibility = Visibility.Collapsed;

            GradualEndCheckBox.Checked += (s, e) => EndTransitionGrid.Visibility = Visibility.Visible;
            GradualEndCheckBox.Unchecked += (s, e) => EndTransitionGrid.Visibility = Visibility.Collapsed;
        }

        private void LoadInterval(FilterInterval interval)
        {
            StartTimeTextBox.Text = interval.StartTime.ToString(@"hh\:mm");
            EndTimeTextBox.Text = interval.EndTime.ToString(@"hh\:mm");
            GreenReductionSlider.Value = interval.GreenReduction;
            BlueReductionSlider.Value = interval.BlueReduction;
            GradualStartCheckBox.IsChecked = interval.GradualStart;
            GradualEndCheckBox.IsChecked = interval.GradualEnd;
            StartTransitionTextBox.Text = interval.StartTransitionDuration.TotalMinutes.ToString();
            EndTransitionTextBox.Text = interval.EndTransitionDuration.TotalMinutes.ToString();
            IsActiveCheckBox.IsChecked = interval.IsActive;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                Result = CreateInterval();
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
            // Validate time format
            if (!TimeSpan.TryParse(StartTimeTextBox.Text, out TimeSpan startTime))
            {
                MessageBox.Show("Please enter a valid start time (HH:MM format).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartTimeTextBox.Focus();
                return false;
            }

            if (!TimeSpan.TryParse(EndTimeTextBox.Text, out TimeSpan endTime))
            {
                MessageBox.Show("Please enter a valid end time (HH:MM format).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                EndTimeTextBox.Focus();
                return false;
            }

            if (endTime <= startTime)
            {
                MessageBox.Show("End time must be after start time.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                EndTimeTextBox.Focus();
                return false;
            }

            // Validate transition durations
            if (GradualStartCheckBox.IsChecked == true)
            {
                if (!int.TryParse(StartTransitionTextBox.Text, out int startDuration) || startDuration <= 0)
                {
                    MessageBox.Show("Please enter a valid start transition duration (positive number).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartTransitionTextBox.Focus();
                    return false;
                }
            }

            if (GradualEndCheckBox.IsChecked == true)
            {
                if (!int.TryParse(EndTransitionTextBox.Text, out int endDuration) || endDuration <= 0)
                {
                    MessageBox.Show("Please enter a valid end transition duration (positive number).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EndTransitionTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private FilterInterval CreateInterval()
        {
            TimeSpan startTime = TimeSpan.Parse(StartTimeTextBox.Text);
            TimeSpan endTime = TimeSpan.Parse(EndTimeTextBox.Text);

            return new FilterInterval
            {
                StartTime = startTime,
                EndTime = endTime,
                GreenReduction = GreenReductionSlider.Value,
                BlueReduction = BlueReductionSlider.Value,
                GradualStart = GradualStartCheckBox.IsChecked == true,
                GradualEnd = GradualEndCheckBox.IsChecked == true,
                StartTransitionDuration = GradualStartCheckBox.IsChecked == true 
                    ? TimeSpan.FromMinutes(int.Parse(StartTransitionTextBox.Text))
                    : TimeSpan.FromMinutes(10),
                EndTransitionDuration = GradualEndCheckBox.IsChecked == true
                    ? TimeSpan.FromMinutes(int.Parse(EndTransitionTextBox.Text))
                    : TimeSpan.FromMinutes(5),
                IsActive = IsActiveCheckBox.IsChecked == true
            };
        }
    }
} 