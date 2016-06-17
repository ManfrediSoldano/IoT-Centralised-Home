using System;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Streams;
using Windows.Devices.Gpio;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        NotHub nothub = new NotHub();
        private const int DOOR_PIN = 16;
        private GpioPin Doorpin;

        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        

        private bool isPreviewing;
        private bool isRecording;
        private DispatcherTimer timer;

        private I2cHelper i2c = new I2cHelper();
        private IoTHelper helper =new IoTHelper();
        private bool active=false;

        public MainPage()
        {
            this.InitializeComponent();
            initVideo_click();

            isRecording = false;
            isPreviewing = false;
            InitGPIO();

            timer = new Windows.UI.Xaml.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(8000);
            timer.Tick += Timer_Tick;
            timer.Start();

          

        }


        private void InitGPIO()
        {
            status.Text = status.Text + "\r\n" + "Inizialising GPIO";
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                status.Text = status.Text = status.Text + "\r\n" + "There is no GPIO controller on this device.";
                return;
            }

            Doorpin = gpio.OpenPin(DOOR_PIN);
            status.Text = status.Text + "\r\n" + "Doorpin created.";
            if (Doorpin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                Doorpin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                Doorpin.SetDriveMode(GpioPinDriveMode.Input);

            status.Text = status.Text + "\r\n" + "GPIO pins initialized correctly.";

            Doorpin.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            Doorpin.ValueChanged += buttonPin_ValueChanged;



        }


        private void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // need to invoke UI updates on the UI thread because this event
            // handler gets invoked on a separate thread.
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {
                if (e.Edge == GpioPinEdge.FallingEdge)
                {

                    status.Text = status.Text + "\r\n" + "Door open, shooting photo";
                    takePhoto();
                    status.Text = status.Text + "\r\n" + "Photo shooted";
                    nothub.sendNotification();

                }
                else
                {
                    status.Text = status.Text + "\r\n" + "door closed";
                }
            });
        }



        private async void initVideo_click()
        {
            // Disable all buttons until initialization completes



            try
            {
                if (mediaCapture != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await mediaCapture.StopPreviewAsync();
                        captureImage.Source = null;

                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        isRecording = false;

                    }
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                status.Text = status.Text = status.Text + "\r\n" + "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);

                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                status.Text = status.Text = status.Text + "\r\n" + "Camera preview succeeded";

                // Enable buttons for video and photo capture


                // Enable Audio Only Init button, leave the video init button disabled

            }
            catch (Exception ex)
            {
                status.Text = status.Text = status.Text + "\r\n" + "Unable to initialize camera for audio/video mode: " + ex.Message;
            }
        }



        public async void takePhoto()
        {
            try
            {


                captureImage.Source = null;
                string photofilename = "foto";
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    photofilename, CreationCollisionOption.GenerateUniqueName);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

                status.Text = status.Text + "\r\n" + "Take Photo succeeded";

                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                status.Text = status.Text + "\r\n" + ex.Message;
                Cleanup();
            }
            finally
            {


            }
        }


        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    status.Text = status.Text = status.Text + "\r\n" + "MediaCaptureFailed: " + currentFailure.Message;

                    if (isRecording)
                    {
                        await mediaCapture.StopRecordAsync();
                        status.Text += "\n Recording Stopped";
                    }
                }
                catch (Exception)
                {
                }
                finally
                {

                    status.Text += status.Text = status.Text + "\r\n" + "\nCheck if camera is diconnected. Try re-launching the app";
                }
            });
        }

        private async void Cleanup()
        {
            if (mediaCapture != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                    captureImage.Source = null;

                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await mediaCapture.StopRecordAsync();
                    isRecording = false;

                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }

        }

        private async void Timer_Tick(object sender, object e)
        {
            if (!active)
            {
                await i2c.Send(0);
                double temp = i2c.received_data[0] + ((double)i2c.received_data[1] / 100);
                helper.SendDeviceToCloudMessagesAsync(temp);
                status.Text = status.Text + "\r\n" + "Inviata temperatura all'IOT HUB! Temp: " + temp.ToString();
            }


        }

        private  void luce1_Click(object sender, RoutedEventArgs e)
        {

            active = true;
             i2c.SendWaiting(2);
            active = false;
        }

        private  void luce2_Click(object sender, RoutedEventArgs e)
        {
            active = true;
             i2c.SendWaiting(3);
            active = false;
        }

        private  void luce3_Click(object sender, RoutedEventArgs e)
        {
            active = true;
             i2c.SendWaiting(4);
            active = false;
        }

        private  void luce5_Click(object sender, RoutedEventArgs e)
        {
            active = true;
             i2c.SendWaiting(5);
            active = false;
        }

        private  void luce4_Click(object sender, RoutedEventArgs e)
        {
            active = true;
             i2c.SendWaiting(6);
            active = false;
        }

        private void luce6_Click(object sender, RoutedEventArgs e)
        {
            active = true;
             i2c.SendWaiting(1);
            active = false;
        }
    }
}
