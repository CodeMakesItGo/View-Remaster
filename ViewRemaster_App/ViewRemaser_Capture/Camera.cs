using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;


namespace ViewRemaser
{
    class Camera
    {
        // private IVideoSource ;
        private VideoCaptureDevice _videoSource;
        private string VideoDeviceMoniker = null;
        private VideoCapabilities videoSelected = null;
        public NewFrameEventHandler Video_NewFrame {private get; set; }

        public Camera(NewFrameEventHandler handler)
        {
            Video_NewFrame = handler;
        }
        
        #region Camera

      
        public bool SelectCamera()
        {
            if(_videoSource != null && _videoSource.IsRunning)
            {
                StopCamera();
            }

            VideoCaptureDeviceForm form = new VideoCaptureDeviceForm();

            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // create video source
                VideoDeviceMoniker = form.VideoDeviceMoniker;

                videoSelected = form.VideoDevice.VideoResolution;

                // stop current video source
                StopCamera();

                // start new video source
                StartCamera();
                return true;
            }
            return false;
        }

        public void SetExposure(int i, bool auto = false)
        {
            if (_videoSource.GetCameraPropertyRange(CameraControlProperty.Exposure, out var min, out var max, out _, out var defaultValue, out _))
            {
                if (auto)
                {
                    _videoSource.SetCameraProperty(CameraControlProperty.Exposure, defaultValue, CameraControlFlags.Auto);
                }
                else
                {
                    if (i >= min && i <= max)
                        _videoSource.SetCameraProperty(CameraControlProperty.Exposure, i, CameraControlFlags.Manual);
                }
            }

        }


        private void StartCamera()
        {
            if (VideoDeviceMoniker != null)
            {
                _videoSource = new VideoCaptureDevice(VideoDeviceMoniker)
                {
                    VideoResolution = videoSelected
                };
                _videoSource.NewFrame += Video_NewFrame;
                _videoSource.Start();
            }
        }

        public void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                //_videoSource.WaitForStop();
                _videoSource.NewFrame -= new NewFrameEventHandler(Video_NewFrame);
                VideoDeviceMoniker = null;
            }
        }

        internal void GetExposureRange(out int _min, out int _max, out int _defaultValue)
        {
            _videoSource.GetCameraPropertyRange(CameraControlProperty.Exposure, out var min, out var max, out _, out var defaultValue, out _);
            _min = min;
            _max = max;
            _defaultValue = defaultValue;
        }

        internal void LockCameraProperties(bool l = true)
        {
            var properties = Enum.GetValues(typeof(CameraControlProperty));
            foreach(var p in properties)
            {
                _videoSource.GetCameraProperty((CameraControlProperty)p, out var v, out var c);
                if(c != CameraControlFlags.None)
                    _videoSource.SetCameraProperty((CameraControlProperty)p, v, l ? CameraControlFlags.Manual : CameraControlFlags.Auto);
            }
        }
        #endregion
    }
}
