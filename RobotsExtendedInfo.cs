using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace RobotsExtended
{
    public class RobotsExtendedInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "RobotsExtended";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRobot;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Extension for Robots plugin";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("1144ad19-a43a-4b2b-870f-19ab431ca98f");
            }
        }

        public override string AuthorName
        {
            get
            {
                return "Victor (Yu Chieh) Lin";
            }
        }
        public override string AuthorContact
        {
            get
            {
                return "https://github.com/lin-ycv/RobotsExtended";
            }
        }

        static readonly Version Ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public override string Version
        {
            get
            {
                return Ver.ToString();
            }
        }
    }
}
