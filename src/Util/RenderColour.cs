namespace RobotsExtended.Util
{
    public class RenderColour : GH_CustomPreviewComponent
    {
        public RenderColour()
        //: base("Render Colour", "Colour",
        //    "Creates colour list for custom preview",
        //    "Robots", "Utility")
        //{ }
        : base()
        {
            Name = "Render Colour";
            NickName = "Colour";
            Description = "Creates colour list for custom preview";
            Category = "Robots";
            SubCategory = "Utility";
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
        public override Guid ComponentGuid => new("{47AEFE2F-21CD-4F92-ABB5-14D136956102}");
        protected override Bitmap Icon => Properties.Resources.Colourful;

        private readonly static Color lightBlack = Color.FromArgb(92, 92, 92);
        private readonly static string[] robotLabels = ["Base", "A1", "A2", "A3", "A4", "A5", "A6", "Tool"];

        private BoundingBox _boundingBox;
        public override BoundingBox ClippingBox => _boundingBox;
        private List<GH_CustomPreviewItem> _items;
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Bake Mesh", "Bake", "Bake coloured robot mesh", GH_ParamAccess.item, false);
            pManager.AddMeshParameter("Robot Mesh", "Mesh", "Robot mesh to colour", GH_ParamAccess.list);
            pManager.AddColourParameter("Robot Base", "Base", "Robot base colour", GH_ParamAccess.item, lightBlack);
            pManager.AddColourParameter("Robot Body", "Body", "Robot body colour", GH_ParamAccess.item, Color.Orange /*Color.FromArgb(255,132,0)*/);
            pManager.AddColourParameter("End Effector", "Tool", "End Effector (Tool) colour", GH_ParamAccess.item, Color.LightGray);
            pManager.AddColourParameter("External Base", "ExBase", "External Base colour", GH_ParamAccess.item, Color.LightGray);
            pManager.AddColourParameter("External Body", "ExBody", "External Base colour", GH_ParamAccess.item, Color.Orange);
            pManager[0].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Coloured Mesh", GH_ParamAccess.list);
            pManager.AddColourParameter("Colour", "C", "", GH_ParamAccess.list);
            pManager.AddTextParameter("Labels", "L", "", GH_ParamAccess.list);
        }

        protected override void BeforeSolveInstance()
        {
            _items = [];
            _boundingBox = BoundingBox.Empty;
            //meshes.Clear();
            //base.BeforeSolveInstance();
        }
        override protected void AfterSolveInstance()
        { }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Mesh> meshes = [];
            DA.GetDataList(1, meshes);
            if (meshes.Count < 8) return;
            Color[] colors = new Color[meshes.Count];
            string[] labels = new string[meshes.Count];
            Array.Copy(robotLabels, 0, labels, labels.Length - 8, 8);

            // Robot color
            Color rBase = new();
            DA.GetData(2, ref rBase);
            colors[colors.Length - 8] = rBase;
            Append(meshes[colors.Length - 8], rBase);
            Color body = new();
            DA.GetData(3, ref body);
            for (int i = -3; i > -8; i--)
            {
                colors[colors.Length + i] = body;
                Append(meshes[colors.Length + i], body);
            }
            colors[colors.Length - 2] = lightBlack;
            Append(meshes[colors.Length - 2], lightBlack);
            // Tool Color
            Color tool = new();
            DA.GetData(4, ref tool);
            colors[colors.Length - 1] = tool;
            Append(meshes[colors.Length - 1], tool);
            // External Axis Color
            if (colors.Length > 8)
            {
                labels[0] = "ExBase";
                DA.GetData(5, ref colors[0]);
                Append(meshes[0], colors[0]);
                Color exBody = new();
                DA.GetData(6, ref exBody);
                for (int i = 1; i < colors.Length - 8; i++)
                {
                    labels[i] = $"E{i}";
                    if (i == colors.Length - 9)
                    {
                        colors[i] = lightBlack;
                        Append(meshes[i], lightBlack);
                    }
                    else
                    {
                        colors[i] = exBody;
                        Append(meshes[i], exBody);
                    }
                }
            }

            DA.SetDataList(0, meshes);
            DA.SetDataList(1, colors);
            DA.SetDataList(2, labels);

            bool bake = false;
            DA.GetData(0, ref bake);
            if (!bake) return;

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            int mainIndex = doc.Layers.FindByFullPath("RobotsExt", -1);
            if (mainIndex == -1)
                mainIndex = doc.Layers.Add("RobotsExt", lightBlack);
            Guid mainId = doc.Layers[mainIndex].Id;
            for (int i = 0; i < labels.Length; i++)
            {
                if (!meshes[i].IsValid) continue;
                int index = doc.Layers.FindByFullPath($"RobotsExt::{labels[i]}", -1);
                if (index == -1)
                {
                    index = doc.Layers.Add($"{labels[i]}", colors[i]);
                    Layer layer = doc.Layers[index];
                    layer.ParentLayerId = mainId;
                }
                ObjectAttributes att = new() { LayerIndex = index };
                doc.Objects.AddMesh(meshes[i].Value, att);
            }

            void Append(GH_Mesh m, Color c)
            {
                if (!m.IsValid) return;
                GH_Material material = new(c);
                GH_CustomPreviewItem item = default;
                item.Geometry = m;
                item.Shader = material.Value;
                item.Colour = material.Value.Diffuse;
                item.Material = material;
                _items.Add(item);
                _boundingBox.Union(m.Boundingbox);
            }
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (this.Locked || _items.Count == 0 || args.Document.IsRenderMeshPipelineViewport(args.Display))
                return;
            if (this.Attributes.Selected)
            {
                GH_PreviewMeshArgs args2 = new(args.Viewport, args.Display, args.ShadeMaterial_Selected, args.MeshingParameters);
                foreach (GH_CustomPreviewItem item in _items)
                    item.Geometry.DrawViewportMeshes(args2);
                //for (int i = 0; i < meshes.Count; i++)
                //    meshes[i].DrawViewportMeshes(args2);
                return;
            }
            foreach (GH_CustomPreviewItem item in _items)
            {
                GH_PreviewMeshArgs args2 = new(args.Viewport, args.Display, item.Shader, args.MeshingParameters);
                item.Geometry.DrawViewportMeshes(args2);
            }
            //for (int i = 0; i < meshes.Count; i++)
            //{
            //    GH_PreviewMeshArgs args2 = new(args.Viewport, args.Display, new DisplayMaterial(colors[i]), args.MeshingParameters);
            //    meshes[i].DrawViewportMeshes(args2);
            //}
            //for(int i = 0; i<meshes.Count; i++)
            //    args.Display.DrawMeshShaded(meshes[i].Value, new DisplayMaterial(colors[i]));
            //base.DrawViewportMeshes(args);
        }
        //public void AppendCustomGeometry(GH_RenderArgs args)
        //{
        //    if (_items == null || _items.Count == 0)
        //    {
        //        return;
        //    }

        //    GH_Document gH_Document = OnPingDocument();
        //    if (gH_Document != null && (gH_Document.PreviewMode == GH_PreviewMode.Disabled || _items.Count == 0 || gH_Document.PreviewMode == GH_PreviewMode.Wireframe))
        //        return;

        //    foreach (var item in _items)
        //        item.PushToRenderPipeline(args);
        //}

        [Obsolete] // For Rhino 7
        public override void AppendRenderGeometry(GH_RenderArgs args)
        {
            GH_Document gH_Document = OnPingDocument();
            if (gH_Document != null && (gH_Document.PreviewMode == GH_PreviewMode.Disabled || _items.Count == 0 || gH_Document.PreviewMode == GH_PreviewMode.Wireframe))
                return;
            foreach (var item in _items)
                item.PushToRenderPipeline(args);
                //args.Geomety.Add(item.Value,args.MaterialNormal);
            //base.AppendRenderGeometry(args);
        }

    }
}
