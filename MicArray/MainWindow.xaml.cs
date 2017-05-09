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
using Microsoft.Kinect;
using System.Windows.Threading;
using System.IO;
using System.Threading;

namespace MicArray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        WriteableBitmap colorBitmap;
        byte[] colorPixels;
        // Skeleton[] totalSkeleton = new Skeleton[6];
        Stream audioStream;
        string wavfilename = "c:\\Users\\Sachin\\Desktop\\KinectAudio.wav";

        double degrees;

        Skeleton[] totalSkeleton = new Skeleton[6];



        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(windowLoaded);


            ///////
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();



            ////Beam Angle Marker

            

        }

        //////--
        private void timer_Tick(object sender, EventArgs e)
        {
            if (mediaElement.Source != null && mediaElement.NaturalDuration.HasTimeSpan)
            {
                start_button.IsEnabled = false;
                play_button.IsEnabled = false;

                progressBar.Minimum = 0;
                progressBar.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                progressBar.Value = mediaElement.Position.TotalSeconds;

                progress.Text = (Math.Ceiling(mediaElement.Position.TotalSeconds)).ToString() + " Secs";
            }

        }
        //////--


        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            this.sensor = KinectSensor.KinectSensors[0];
            this.sensor.SkeletonStream.Enable();
            this.sensor.ColorStream.Enable();
            this.sensor.AllFramesReady += allFramesReady;

            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.image_view.Source = this.colorBitmap;

            this.sensor.AudioSource.SoundSourceAngleChanged += soundSourceAngleChanged;

            this.sensor.AudioSource.BeamAngleChanged += beamAngleChanged;


            this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            this.sensor.SkeletonStream.Enable();
            this.sensor.SkeletonFrameReady += skeletonFrameReady;


            // start the sensor.
            this.sensor.Start();

          //  progressBar.Maximum = 100;
          //  progressBar.Value = 10;

        }

        void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                skeletonFrame.CopySkeletonDataTo(totalSkeleton);
                Skeleton firstSkeleton = (from trackskeleton in totalSkeleton
                                          where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                                          select trackskeleton).FirstOrDefault();
                if (firstSkeleton == null)
                {
                    return;
                }
                if (firstSkeleton.Joints[JointType.HandRight].TrackingState ==
                   JointTrackingState.Tracked)
                {
                    this.MapJointsWithUIElement(firstSkeleton);
                }
            }
        }


        private void MapJointsWithUIElement(Skeleton skeleton)
        {
            Point mappedPoint = ScalePosition(skeleton.Joints[JointType.HandRight].Position);

            double min = 10;
            double max = 250;

            double percentage = mappedPoint.X / 640;

            double location = min + percentage * (max - min);

            Canvas.SetLeft(righthand, location);

         //   beam_angle_input.Text = mappedPoint.X.ToString();

           // Canvas.SetTop(righthand, mappedPoint.Y);
            //this.textBox1.Text = "x="+mappedPoint.X+", y="+mappedPoint.Y;
        }

        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.
                      MapSkeletonPointToDepthPoint(skeletonPoint, DepthImageFormat.
                                 Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }


        private void startAudioStreamBtn_Click(object sender, RoutedEventArgs e)
        {

            audioStream = this.sensor.AudioSource.Start();
        }

        private void stopAudioStreamBtn_Click(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.Stop();
        }

        void beamAngleChanged(object sender, BeamAngleChangedEventArgs e)
        {
            //this.sensor.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
            //this.sensor.AudioSource.ManualBeamAngle = 30;

            this.beam_angle.Text = e.Angle.ToString();

            int pos = 0;
            double percentage = e.Angle / 50;
            pos = (int) (130 + (120*percentage)); //120 is 240/2 where 240 is width of the image view

            ////
            Canvas.SetLeft(beamAngleMarker, pos);

        }

        void soundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            this.sound_source_angle.Text = e.Angle.ToString();
            this.confidence_angle.Text = e.ConfidenceLevel.ToString();

            int pos = 0;
            double percentage = e.Angle / 50;
            pos = (int)(130 + (120 * percentage)); //120 is 240/2 where 240 is width of the image view

            ////
            Canvas.SetLeft(SourceAngleMarker, pos);

        }

        void allFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (null == imageFrame)
                    return;
                imageFrame.CopyPixelDataTo(colorPixels);
                int stride = imageFrame.Width * imageFrame.BytesPerPixel;

                this.colorBitmap.WritePixels(
          new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                   this.colorPixels,
                   stride,
                   0);
            }
        }

        public void RecordAudio()
        {

            //To check if the recording exists
            if (File.Exists(wavfilename))
            {
               
                MessageBoxResult result = MessageBox.Show("The Recording already exists! Do you want to delete the previous recording and proceed?", 
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    File.SetAttributes(wavfilename, FileAttributes.Normal);
                    File.Delete(wavfilename);
                }
            }
           

            int recordingLength = (int)30 * 2 * 16000; //30 seconds of recording time.

            //-------
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                this.progressBar.Maximum =  recordingLength ;
               
                // Do all the ui thread updates here
                
            }));

            byte[] buffer = new byte[1024];
            Boolean startAudioStreamHere = false;

            

            using (FileStream fileStream = new FileStream(wavfilename, FileMode.Create))
            {
                WriteWavHeader(fileStream, recordingLength);
                if (audioStream == null)
                {
                    startAudioStreamHere = true;
                    audioStream = this.sensor.AudioSource.Start();
                }
                int count, totalCount = 0;
                while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0
           && totalCount < recordingLength)
                {
                    fileStream.Write(buffer, 0, count);
                    totalCount += count;

                    //////
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                        this.progress.Text = (totalCount).ToString() + "bytes";
                        this.progressBar.Value = totalCount; // Do all the ui thread updates here
                    }));

                }
                if (startAudioStreamHere == true)
                {
                    this.sensor.AudioSource.Stop();

                

                }
            }

        }

        static void WriteWavHeader(Stream stream, int dataLength)
        {
            using (var memStream = new MemoryStream(64))
            {
                int cbFormat = 18; //sizeof(WAVEFORMATEX)
                WAVEFORMATEX format = new WAVEFORMATEX()
                {
                    wFormatTag = 1,
                    nChannels = 1,
                    nSamplesPerSec = 16000,
                    nAvgBytesPerSec = 32000,
                    nBlockAlign = 2,
                    wBitsPerSample = 16,
                    cbSize = 0
                };
                using (var binarywriter = new BinaryWriter(memStream))
                {
                    //RIFF header
                    WriteString(memStream, "RIFF");
                    binarywriter.Write(dataLength + cbFormat + 4);
                    WriteString(memStream, "WAVE");
                    WriteString(memStream, "fmt ");
                    binarywriter.Write(cbFormat);
                    //WAVEFORMATEX
                    binarywriter.Write(format.wFormatTag);
                    binarywriter.Write(format.nChannels);
                    binarywriter.Write(format.nSamplesPerSec);
                    binarywriter.Write(format.nAvgBytesPerSec);
                    binarywriter.Write(format.nBlockAlign);
                    binarywriter.Write(format.wBitsPerSample);
                    binarywriter.Write(format.cbSize);
                    //data header
                    WriteString(memStream, "data");
                    binarywriter.Write(dataLength);
                    memStream.WriteTo(stream);
                }
            }
        }

        struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }


        static void WriteString(Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }




        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            var audioThread = new Thread(new ThreadStart(RecordAudio));
            audioThread.SetApartmentState(ApartmentState.MTA);
            audioThread.Start();
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            //To check if the recording exists
            if (!File.Exists(wavfilename))
            {
                MessageBoxResult result = MessageBox.Show("No Recording exists to play! Do you want to start Recording?",
                 "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var audioThread = new Thread(new ThreadStart(RecordAudio));
                    audioThread.SetApartmentState(ApartmentState.MTA);
                    audioThread.Start();
                }
            }

            if (!string.IsNullOrEmpty(wavfilename) && File.Exists(wavfilename))
            {
                mediaElement.Source = new Uri(wavfilename, UriKind.RelativeOrAbsolute);
                mediaElement.LoadedBehavior = MediaState.Play;
               // this.progress.Text = mediaElement.GetValue.ToString();
                mediaElement.UnloadedBehavior = MediaState.Close;
            }
        }

        private void noiseSuppression_Checked(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.NoiseSuppression = true;
        }
        private void echoCancellation_Checked(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.CancellationOnly;
            this.sensor.AudioSource.EchoCancellationSpeakerIndex = 0;
        }
        private void gainControl_Checked(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.AutomaticGainControlEnabled = true;
        }

        private void gainControl_Unchecked(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.AutomaticGainControlEnabled = false;
        }
        private void echoCancellation_Unchecked(object sender, RoutedEventArgs e)
        { 
     this.sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
}
    private void noiseSuppression_Unchecked(object sender, RoutedEventArgs e)
    {
        this.sensor.AudioSource.NoiseSuppression = false;
    }

        private void beamManual_Checked(object sender, RoutedEventArgs e)
        {

            this.sensor.AudioSource.BeamAngleMode = BeamAngleMode.Manual;

            double x = Canvas.GetLeft(righthand);

            double beam_manual = (x - 10) / 240 * 100;

            beam_manual = beam_manual - 50;

            ////
            
            beam_angle_manual.Text = beam_manual.ToString();
            
            this.sensor.AudioSource.ManualBeamAngle = beam_manual;
            
           
        }

        private void beamManual_Unchecked(object sender, RoutedEventArgs e)
        {
            this.sensor.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
        }
    }
}
