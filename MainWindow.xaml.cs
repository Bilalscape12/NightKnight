using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NightKnight
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        private DispatcherTimer scheduleTimer = null!;
        private DispatcherTimer animationTimer = null!;
        private DispatcherTimer statusTimer = null!;
        private ObservableCollection<FilterInterval> intervals = [];
        private double currentGreenReduction = 0.0;
        private double currentBlueReduction = 0.0;
        private double targetGreenReduction = 0.0;
        private double targetBlueReduction = 0.0;
        private const double ANIMATION_STEP = 0.016; // ~60 FPS
        private double animationProgress = 0.0;
        private bool isAnimating = false;
        private bool scheduleEnabled = false;
        private bool isInTransition = false;
        private double userLatitude = 40.7128; // Default to New York
        private double userLongitude = -74.0060;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeTimers();
            InitializeUI();
            LoadIntervals();
            
            // Add window closing event handler
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveIntervals();
            SaveSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingsManager.LoadSettings();
            scheduleEnabled = settings.ScheduleEnabled;
            userLatitude = settings.UserLatitude;
            userLongitude = settings.UserLongitude;
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                ScheduleEnabled = scheduleEnabled,
                UserLatitude = userLatitude,
                UserLongitude = userLongitude
            };
            SettingsManager.SaveSettings(settings);
        }

        private void LoadIntervals()
        {
            var savedIntervals = SettingsManager.LoadIntervals();
            
            if (savedIntervals.Count > 0)
            {
                // Load saved intervals
                foreach (var interval in savedIntervals)
                {
                    intervals.Add(interval);
                }
            }
            else
            {
                // Load default schedule if no saved intervals exist
                LoadDefaultSchedule();
            }
            
            SortIntervals();
            
            // Add collection changed event handler to auto-save
            intervals.CollectionChanged += (s, e) => SaveIntervals();
        }

        private void SaveIntervals()
        {
            SettingsManager.SaveIntervals(intervals);
        }

        private void InitializeTimers()
        {
            // Schedule timer - starts with 1 minute interval, will be adjusted dynamically
            scheduleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            scheduleTimer.Tick += ScheduleTimer_Tick;

            // Animation timer for smooth transitions
            animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(ANIMATION_STEP)
            };
            animationTimer.Tick += AnimationTimer_Tick;

            // Status timer - updates every second
            statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();
        }

        private void InitializeUI()
        {
            ScheduleListView.ItemsSource = intervals;
            
            GreenSlider.ValueChanged += (s, e) =>
            {
                GreenText.Text = $"Green Reduction: {GreenSlider.Value:0.00}";
            };
            BlueSlider.ValueChanged += (s, e) =>
            {
                BlueText.Text = $"Blue Reduction: {BlueSlider.Value:0.00}";
            };

            EnableScheduleCheckBox.Checked += (s, e) => 
            {
                scheduleEnabled = true;
                scheduleTimer.Start();
                SaveSettings();
            };
            EnableScheduleCheckBox.Unchecked += (s, e) => 
            {
                scheduleEnabled = false;
                scheduleTimer.Stop();
                SaveSettings();
            };

            // Set initial checkbox state
            EnableScheduleCheckBox.IsChecked = scheduleEnabled;
        }

        private void LoadDefaultSchedule()
        {
            // Example schedule - users can modify this
            intervals.Add(new FilterInterval
            {
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(20),
                GreenReduction = 0.1,
                BlueReduction = 0.3,
                GradualStart = false,
                GradualEnd = true,
                EndTransitionDuration = TimeSpan.FromMinutes(1)
            });

            intervals.Add(new FilterInterval
            {
                StartTime = TimeSpan.FromHours(20),
                EndTime = TimeSpan.FromHours(8),
                GreenReduction = 0.3,
                BlueReduction = 0.5,
                GradualStart = false,
                GradualEnd = true,
                EndTransitionDuration = TimeSpan.FromMinutes(1)
            });
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            CurrentTimeText.Text = currentTime.ToString(@"hh\:mm");

            FilterInterval? activeInterval = GetActiveInterval(currentTime);
            if (activeInterval != null)
            {
                string intervalText;
                if (activeInterval.StartTime > activeInterval.EndTime)
                {
                    // Overnight interval
                    intervalText = $"{activeInterval.StartTime:hh\\:mm} - {activeInterval.EndTime:hh\\:mm} (Overnight)";
                }
                else
                {
                    // Normal interval
                    intervalText = $"{activeInterval.StartTime:hh\\:mm} - {activeInterval.EndTime:hh\\:mm}";
                }
                ActiveFilterText.Text = intervalText;
            }
            else
            {
                ActiveFilterText.Text = "None";
            }

            CurrentGreenText.Text = $"{currentGreenReduction:P0}";
            CurrentBlueText.Text = $"{currentBlueReduction:P0}";
        }

        private void ScheduleTimer_Tick(object? sender, EventArgs e)
        {
            if (!scheduleEnabled || isAnimating) return;

            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            FilterInterval? activeInterval = GetActiveInterval(currentTime);
            FilterInterval? nextInterval = GetNextInterval(currentTime);
            bool wasInTransition = isInTransition;
            isInTransition = false;

            if (activeInterval != null)
            {
                double targetGreen = activeInterval.GreenReduction;
                double targetBlue = activeInterval.BlueReduction;

                // Check if we need gradual transitions
                if (activeInterval.GradualStart && IsInStartTransition(currentTime, activeInterval))
                {
                    isInTransition = true;
                    double progress = GetStartTransitionProgress(currentTime, activeInterval);
                    targetGreen *= progress;
                    targetBlue *= progress;
                }
                else if (activeInterval.GradualEnd && IsInEndTransition(currentTime, activeInterval))
                {
                    isInTransition = true;
                    double progress = GetEndTransitionProgress(currentTime, activeInterval);
                    
                    // Check if there's a next interval that starts when this one ends
                    if (nextInterval != null && nextInterval.StartTime == activeInterval.EndTime)
                    {
                        // Transition to next interval's settings instead of zero
                        targetGreen = activeInterval.GreenReduction + (nextInterval.GreenReduction - activeInterval.GreenReduction) * (1 - progress);
                        targetBlue = activeInterval.BlueReduction + (nextInterval.BlueReduction - activeInterval.BlueReduction) * (1 - progress);
                    }
                    else
                    {
                        // Transition to zero (no next interval)
                        targetGreen *= progress;
                        targetBlue *= progress;
                    }
                }

                if (Math.Abs(targetGreen - currentGreenReduction) > 0.001 || 
                    Math.Abs(targetBlue - currentBlueReduction) > 0.001)
                {
                    StartSmoothTransition(targetGreen, targetBlue);
                }
            }
            else
            {
                // No active interval, gradually reduce to zero
                if (Math.Abs(currentGreenReduction) > 0.001 || Math.Abs(currentBlueReduction) > 0.001)
                {
                    StartSmoothTransition(0.0, 0.0);
                }
            }

            // Adjust timer interval based on whether we're in a transition
            AdjustScheduleTimerInterval(wasInTransition, isInTransition);
        }

        private FilterInterval? GetNextInterval(TimeSpan currentTime)
        {
            return intervals
                .Where(interval => interval.IsActive && interval.StartTime > currentTime)
                .OrderBy(interval => interval.StartTime)
                .FirstOrDefault();
        }

        private void AdjustScheduleTimerInterval(bool wasInTransition, bool isInTransition)
        {
            if (isInTransition && !wasInTransition)
            {
                // Entering transition - use faster updates
                scheduleTimer.Interval = TimeSpan.FromSeconds(1);
            }
            else if (!isInTransition && wasInTransition)
            {
                // Exiting transition - return to normal updates
                scheduleTimer.Interval = TimeSpan.FromMinutes(1);
            }
        }

        private FilterInterval? GetActiveInterval(TimeSpan currentTime)
        {
            return intervals.FirstOrDefault(interval => 
                interval.IsActive && 
                IsTimeInInterval(currentTime, interval));
        }

        private static bool IsTimeInInterval(TimeSpan currentTime, FilterInterval interval)
        {
            // Handle intervals that cross midnight
            if (interval.StartTime > interval.EndTime)
            {
                // Interval crosses midnight (e.g., 21:00 to 08:00)
                return currentTime >= interval.StartTime || currentTime <= interval.EndTime;
            }
            else
            {
                // Normal interval within same day (e.g., 10:00 to 18:00)
                return currentTime >= interval.StartTime && currentTime <= interval.EndTime;
            }
        }

        private static bool IsInStartTransition(TimeSpan currentTime, FilterInterval interval)
        {
            if (!interval.GradualStart) return false;
            
            TimeSpan transitionEnd = interval.StartTime + interval.StartTransitionDuration;
            
            // Handle transition that crosses midnight
            if (transitionEnd > TimeSpan.FromHours(24))
            {
                transitionEnd -= TimeSpan.FromHours(24);
                return currentTime >= interval.StartTime || currentTime <= transitionEnd;
            }
            else
            {
                return currentTime >= interval.StartTime && currentTime <= transitionEnd;
            }
        }

        private static bool IsInEndTransition(TimeSpan currentTime, FilterInterval interval)
        {
            if (!interval.GradualEnd) return false;
            
            TimeSpan transitionStart = interval.EndTime - interval.EndTransitionDuration;
            
            // Handle transition that crosses midnight
            if (transitionStart < TimeSpan.Zero)
            {
                transitionStart = TimeSpan.FromHours(24) + transitionStart;
                return currentTime >= transitionStart || currentTime <= interval.EndTime;
            }
            else
            {
                return currentTime >= transitionStart && currentTime <= interval.EndTime;
            }
        }

        private static double GetStartTransitionProgress(TimeSpan currentTime, FilterInterval interval)
        {
            TimeSpan elapsed;
            
            if (currentTime >= interval.StartTime)
            {
                // Normal case: current time is after start time
                elapsed = currentTime - interval.StartTime;
            }
            else
            {
                // Crossed midnight: current time is before start time but interval is active
                elapsed = (TimeSpan.FromHours(24) - interval.StartTime) + currentTime;
            }
            
            return Math.Min(1.0, elapsed.TotalMinutes / interval.StartTransitionDuration.TotalMinutes);
        }

        private static double GetEndTransitionProgress(TimeSpan currentTime, FilterInterval interval)
        {
            TimeSpan remaining;
            
            if (currentTime <= interval.EndTime)
            {
                // Normal case: current time is before end time
                remaining = interval.EndTime - currentTime;
            }
            else
            {
                // Crossed midnight: current time is after end time but interval is still active
                remaining = (TimeSpan.FromHours(24) - currentTime) + interval.EndTime;
            }
            
            return Math.Max(0.0, remaining.TotalMinutes / interval.EndTransitionDuration.TotalMinutes);
        }

        private void StartSmoothTransition(double targetGreen, double targetBlue)
        {
            targetGreenReduction = targetGreen;
            targetBlueReduction = targetBlue;
            animationProgress = 0.0;
            isAnimating = true;
            
            if (!animationTimer.IsEnabled)
            {
                animationTimer.Start();
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            animationProgress += ANIMATION_STEP / 0.5; // 0.5 second transition
            
            if (animationProgress >= 1.0)
            {
                animationProgress = 1.0;
                animationTimer.Stop();
                isAnimating = false;
            }

            // Smooth easing function
            double easedProgress = animationProgress < 0.5 
                ? 2 * animationProgress * animationProgress 
                : 1 - Math.Pow(-2 * animationProgress + 2, 2) / 2;

            currentGreenReduction += (targetGreenReduction - currentGreenReduction) * easedProgress;
            currentBlueReduction += (targetBlueReduction - currentBlueReduction) * easedProgress;

            ApplyLightFilter(currentGreenReduction, currentBlueReduction);
        }

        // GUI Event Handlers
        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            StartSmoothTransition(GreenSlider.Value, BlueSlider.Value);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            StartSmoothTransition(0.0, 0.0);
        }

        private void AddInterval_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new IntervalDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                intervals.Add(dialog.Result);
                SortIntervals();
                SaveIntervals();
            }
        }

        private void EditInterval_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduleListView.SelectedItem is FilterInterval selectedInterval)
            {
                var dialog = new IntervalDialog(selectedInterval)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    int index = intervals.IndexOf(selectedInterval);
                    intervals[index] = dialog.Result;
                    SortIntervals();
                    SaveIntervals();
                }
            }
            else
            {
                MessageBox.Show("Please select an interval to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteInterval_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduleListView.SelectedItem is FilterInterval selectedInterval)
            {
                var result = MessageBox.Show("Are you sure you want to delete this interval?", "Confirm Delete", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    intervals.Remove(selectedInterval);
                    SaveIntervals();
                }
            }
            else
            {
                MessageBox.Show("Please select an interval to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAllIntervals_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all intervals?", "Confirm Clear", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                intervals.Clear();
                SaveIntervals();
            }
        }

        private void SunsetSunrisePreset_Click(object sender, RoutedEventArgs e)
        {
            var locationDialog = new LocationDialog(userLatitude, userLongitude)
            {
                Owner = this
            };

            if (locationDialog.ShowDialog() == true)
            {
                userLatitude = locationDialog.Latitude;
                userLongitude = locationDialog.Longitude;
                CreateSunsetSunrisePreset();
                SaveSettings();
            }
        }

        private void CreateSunsetSunrisePreset()
        {
            try
            {
                var today = DateTime.Today;
                var (sunrise, sunset) = SunCalculator.CalculateSunTimes(today, userLatitude, userLongitude);
                
                // Calculate the duration of sunrise and sunset
                TimeSpan sunsetDuration = SunCalculator.CalculateSunsetDuration(today, userLatitude, userLongitude);
                TimeSpan sunriseDuration = SunCalculator.CalculateSunriseDuration(today, userLatitude, userLongitude);

                // Clear existing intervals
                // intervals.Clear();

                // Create single overnight interval from sunset to sunrise
                intervals.Add(new FilterInterval
                {
                    StartTime = sunset,
                    EndTime = sunrise,
                    GreenReduction = 0.2,
                    BlueReduction = 0.5,
                    GradualStart = true,
                    GradualEnd = true,
                    StartTransitionDuration = sunsetDuration,
                    EndTransitionDuration = sunriseDuration,
                    IsActive = true
                });

                SortIntervals();
                SaveIntervals();

                MessageBox.Show($"Sunset/Sunrise preset created for your location!\n\n" +
                               $"Today's times:\n" +
                               $"Sunrise: {sunrise:hh\\:mm}\n" +
                               $"Sunset: {sunset:hh\\:mm}\n" +
                               $"Sunset Duration: {sunsetDuration.TotalMinutes:F0} minutes\n" +
                               $"Sunrise Duration: {sunriseDuration.TotalMinutes:F0} minutes\n\n" +
                               $"The filter will gradually activate during sunset and deactivate during sunrise.",
                               "Preset Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScheduleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable edit and delete buttons based on selection
            bool hasSelection = ScheduleListView.SelectedItem != null;
            // You can add button references here if needed
        }

        private void ScheduleListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the item that was clicked
            var item = GetListViewItemFromPoint(ScheduleListView, e.GetPosition(ScheduleListView));
            
            // If no item was clicked (clicked on empty space), clear selection
            if (item == null)
            {
                ScheduleListView.SelectedItem = null;
            }
        }

        private ListViewItem? GetListViewItemFromPoint(ListView listView, Point point)
        {
            var element = listView.InputHitTest(point) as DependencyObject;
            
            while (element != null && !(element is ListViewItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }
            
            return element as ListViewItem;
        }

        private void SortIntervals()
        {
            var sorted = intervals.OrderBy(i => i.StartTime).ToList();
            intervals.Clear();
            foreach (var interval in sorted)
            {
                intervals.Add(interval);
            }
        }

        private static void ApplyLightFilter(double greenReduction, double blueReduction)
        {
            double gammaMax = 65535.0;
            RAMP ramp = new()
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            for (int i = 0; i < 256; i++)
            {
                double intensity = i / 255.0;

                ramp.Red[i] = (ushort)(gammaMax * intensity);
                ramp.Green[i] = (ushort)(gammaMax * intensity * (1.0 - greenReduction));
                ramp.Blue[i] = (ushort)(gammaMax * intensity * (1.0 - blueReduction));
            }

            IntPtr hdc = GetDC(GetDesktopWindow());
            bool success = SetDeviceGammaRamp(hdc, ref ramp);

            if (!success)
            {
                MessageBox.Show("Failed to apply gamma ramp.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (hdc == IntPtr.Zero)
            {
                MessageBox.Show("Failed to get device context.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}