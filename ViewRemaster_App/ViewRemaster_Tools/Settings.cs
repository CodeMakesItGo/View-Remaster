using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ViewRemaster_Tools
{

    [XmlRoot()]
    public class Settings
    {
        private static Settings instance = null;
        private static readonly string AppTmpFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\View_Remaster";
        private static readonly string SettingsFilePath = AppTmpFolder + "\\settings.xml";

        private Settings() { }

        /// <summary>
        /// Access the Singleton instance
        /// </summary>
        [XmlElement]
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                    Settings.Deserialize();
                }
                return instance;
            }
        }
        [XmlAttribute]
        public string RootPath { get; set; }

        [XmlAttribute]
        public int ReelCrop_X { get; set; }

        [XmlAttribute]
        public int ReelCrop_Y { get; set; }

        [XmlAttribute]
        public int ReelCrop_Width { get; set; }

        [XmlAttribute]
        public int ReelCropCenter_Width { get; set; }

        [XmlAttribute]
        public int TextCrop_X { get; set; }

        [XmlAttribute]
        public int TextCrop_Y { get; set; }

        [XmlAttribute]
        public int TextCrop_Width { get; set; }

        [XmlAttribute]
        public int TextCrop_Height { get; set; }

        [XmlAttribute]
        public int Slide_Gap { get; set; }
                                                                                        
        [XmlAttribute]
        public int Slide_Top { get; set; }

        [XmlAttribute]
        public int SlideText_Top { get; set; }

        [XmlAttribute]
        public int SlideCrop_X { get; set; }

        [XmlAttribute]
        public int SlideCrop_Y { get; set; }

        [XmlAttribute]
        public int SlideCrop_Width { get; set; }

        [XmlAttribute]
        public int SlideCrop_Height { get; set; }

        [XmlAttribute]
        public int SlideCrop_Corner { get; set; }

        public static void SetDefaults()
        {
            instance.ReelCrop_X = 535;
            instance.ReelCrop_Y = 115;
            instance.ReelCrop_Width = 850;
            instance.ReelCropCenter_Width = 90;

            instance.SlideCrop_X = 10;
            instance.SlideCrop_Y = 15;
            instance.SlideCrop_Width = 1070;
            instance.SlideCrop_Height = 980;
            instance.SlideCrop_Corner = 200;

            instance.TextCrop_X = 820;
            instance.TextCrop_Y = 170;
            instance.TextCrop_Width = 270;
            instance.TextCrop_Height = 160;

            instance.SlideText_Top = 1040;
            instance.Slide_Top = 50;
            instance.Slide_Gap = 40;

            instance.RootPath = AppTmpFolder;

            Serialize();
        }

        /// <summary>
        /// Save setting into file
        /// </summary>
        public static void Serialize()
        {
            // Create the directory
            if (!Directory.Exists(AppTmpFolder))
            {
                Directory.CreateDirectory(AppTmpFolder);
            }

            using (TextWriter writer = new StreamWriter(SettingsFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(writer, Settings.Instance);
            }
        }

        /// <summary>
        /// Load setting from file
        /// </summary>
        public static void Deserialize()
        {
            if (!File.Exists(SettingsFilePath))
            {
                // Can't find saved settings, using default vales
                SetDefaults();
                return;
            }
            try
            {
                using (XmlReader reader = XmlReader.Create(SettingsFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    if (serializer.CanDeserialize(reader))
                    {
                        Settings.instance = serializer.Deserialize(reader) as Settings;
                    }
                }
            }
            catch (System.Exception)
            {
                // Failed to load some data, leave the settings to default
                SetDefaults();
            }
        }
    }
}