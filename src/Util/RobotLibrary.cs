namespace RobotsExtended.Util
{
    public class RobotLibrary : GH_Component
    {
        public override Guid ComponentGuid => new("{73D053CD-4013-40D2-BE10-C62F999AB60C}");
        override public GH_Exposure Exposure => GH_Exposure.secondary;
        protected override Bitmap Icon => Properties.Resources.Library;
        public RobotLibrary() : base("Robot Library", "Library", "Double click to open Robot Library directory", "Robots", "Utility") { }
        bool Open = false;
        private static readonly string localPath = FileIO.LocalLibraryPath, downloadPath = FileIO.OnlineLibraryPath;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        { }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path to Local Robot Library", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Path to Downloaded Robot Library", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData(0, localPath);
            DA.SetData(1, downloadPath);

            if (!Open) return;

            bool l, d;
            l = Directory.Exists(localPath);
            d = Directory.Exists(downloadPath);
            if (l)
                OpenDir(localPath);
            if (d)
                OpenDir(downloadPath);
            if (!l && !d)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Library Found");
            Open = false;
        }
        public override void CreateAttributes()
        {
            m_attributes = new LibAttributes(this);
        }
        public class LibAttributes(IGH_Component component) : GH_ComponentAttributes(component)
        {
            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (base.Owner is RobotLibrary comp)
                {
                    comp.Open = true;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }
                return base.RespondToMouseDoubleClick(sender, e);
            }
        }
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Create Local Directory", CreateDir, !Directory.Exists(localPath));
            base.AppendAdditionalMenuItems(menu);
        }

        private void CreateDir(object sender, EventArgs e)
        {
            Directory.CreateDirectory(localPath);
            OpenDir(localPath);
        }

        private void OpenDir(string path)
        {
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
        }
        internal static IEnumerable<string> LibPath()
        {
            yield return localPath;
            yield return downloadPath;
        }
    }
}
