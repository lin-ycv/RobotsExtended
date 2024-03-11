using RC = Robots.Commands;
using System.Xml.Linq;

namespace RobotsExtended.Util
{
    public class LoadCommand : GH_Component
    {
        public LoadCommand() : base("Load command", "Com", "Load predefined commands on the xml config", "Robots", "Components") { }
        public override Guid ComponentGuid => new("{56C786AD-B863-4D81-87FE-6B7F233295A1}");
        protected override Bitmap Icon => Properties.Resources.Command;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of the Command", GH_ParamAccess.item);
            pManager.AddTextParameter("Value", "V", "Value for commands that takes input", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Command", "C", "", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            CommandList();
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null, val = null;
            if (!DA.GetData(0, ref name) || string.IsNullOrEmpty(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No command selected");
                return;
            }
            try
            {
                DA.GetData(1, ref val);
                var command = Load(name, val);
                DA.SetData(0, command);
            }
            catch(Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
            }
        }

        internal void CommandList()
        {
            if (Params.Input[0].SourceCount > 0) return;
            var doc = OnPingDocument();
            GH_ValueList list = new();
            list.CreateAttributes();
            list.Attributes.Pivot = new PointF(
                        Attributes.Pivot.X - list.Attributes.Bounds.Width - Attributes.Bounds.Width / 2,
                        Attributes.Pivot.Y - list.Attributes.Bounds.Height);
            list.ListItems.Clear();
            list.ListItems.AddRange(List().Select(i => new GH_ValueListItem(i, $"\"{i}\"")));
            if (list.ListItems.Count > 0)
                list.ListItems[0].Selected = true;
            doc.AddObject(list, false);
            Params.Input[0].AddSource(list);
            Params.Input[0].CollectData();
        }

        static IEnumerable<string> GetLibraries()
        {
            var previous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in RobotLibrary.LibPath())
            {
                if (!Directory.Exists(path))
                    continue;

                var files = Directory.EnumerateFiles(path, "*.xml");

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);

                    if (!previous.Add(name))
                        continue;

                    yield return file;
                }
            }
        }
        static List<string> List()
        {
            List<string> names = [];

            foreach (var file in GetLibraries())
            {
                var root = XElement.Load(file);
                var elements = root.Elements(XName.Get("Custom"));

                foreach (var element in elements)
                    names.Add(element.Attribute("name").Value);
            }

            return names;
        }

        static RC.Custom Load(string name, string val)
        {
            foreach (var file in GetLibraries())
            {
                var root = XElement.Load(file);
                var elements = root.Elements(XName.Get("Custom"));

                var element = elements.FirstOrDefault(e => e.Attribute("name").Value.Equals(name,StringComparison.InvariantCultureIgnoreCase));

                if (element == null) break;

                var command = element.Element(XName.Get("Command"));
                var code = command.Attribute(XName.Get("Code")).Value;
                var input = command.Attribute(XName.Get("input"))?.Value;
                var manu = command.Attribute(XName.Get("manufacturer"))?.Value;
                if (input == "true")
                {
                    name += val ?? throw new Exception("Value (V) requires input");
                    code = code.Replace("?", val);
                }

                return new RC.Custom(name, Enum.TryParse<Manufacturers>(manu, out var manufacturer)?manufacturer:Manufacturers.All, code,null);
            }

            throw new Exception($"Command \"{name}\" not found");
        }
    }
}
