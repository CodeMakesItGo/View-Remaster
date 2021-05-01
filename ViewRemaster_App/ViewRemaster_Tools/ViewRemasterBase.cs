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

        public readonly string rootPath = @"U:\Users\jason\OneDrive\Projects\MakeItGo\ViewRemaster\Slides";
        public string TemplatePath { get; protected set; } = @"\templates";
        public string ProcessPath { get; protected set; }
        public string FinalPath { get; protected set; }
        public int CurrentSlide { get; set; } = 0;

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
