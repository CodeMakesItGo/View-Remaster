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
        public readonly int ReelWidth = 3264;
        public readonly int ReelHeight = 2448;
        public readonly int SlideWidth = 3264;
        public readonly int SlideHeight = 2448;

        public readonly int OutputWidth = 3840;
        public readonly int OutputHeight = 2160;

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
