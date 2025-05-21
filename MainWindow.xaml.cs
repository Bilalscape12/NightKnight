using System;
using System.Runtime.InteropServices;
using System.Windows;

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

        public MainWindow()
        {
            InitializeComponent();
            GreenSlider.ValueChanged += (s, e) =>
            {
                GreenText.Text = $"Green Reduction: {GreenSlider.Value:0.00}";
            };
            BlueSlider.ValueChanged += (s, e) =>
            {
                BlueText.Text = $"Blue Reduction: {BlueSlider.Value:0.00}";
            };
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            double greenReduction = GreenSlider.Value;
            double blueReduction = BlueSlider.Value;
            ApplyLightFilter(greenReduction, blueReduction);
        }

        private void ApplyLightFilter(double greenReduction, double blueReduction)
        {
            double gammaMax = 65535.0;
            RAMP ramp = new RAMP
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