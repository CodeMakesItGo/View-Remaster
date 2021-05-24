using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewRemaster_Tools
{
    public class ViewRemasterBase
    {
        private string _path;
        public string Path { get => _path; set { ProcessPath = value + @"\process"; FinalPath = value + @"\final"; _path = value; } }

        public string rootPath = "";
        public string ProcessPath { get; protected set; }
        public string FinalPath { get; protected set; }
        public int CurrentSlide { get; set; } = 0;

        //Camera resolution of the shots
        public readonly int ReelWidth = 1920;
        public readonly int ReelHeight = 1080;
        public readonly int SlideWidth = 1600;
        public readonly int SlideHeight = 1200;

        public readonly int OutputWidth = 1920;
        public readonly int OutputHeight = 1080;

        public ViewRemasterBase()
        {
            rootPath = Settings.Instance.RootPath;
        }

        public void UpdatePath(string dir)
        {
            Path = rootPath + "\\" + dir;

            if (Directory.Exists(Path))
            {
                Directory.CreateDirectory(ProcessPath);
                Directory.CreateDirectory(FinalPath);
            }
            else
            {
                Path = "";
            }
        }

        protected void CreatePath(string dir)
        {
            Path = rootPath + "\\" + dir;

            try
            {
                Directory.CreateDirectory(Path);
            }
            catch
            {
                Path = "";
            }
        }
    }
}
