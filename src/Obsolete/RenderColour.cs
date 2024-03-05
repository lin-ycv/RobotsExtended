﻿using GH_IO.Serialization;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotsExtended.Obsolete
{
    [Obsolete("Replace with proper C# instaed of snippet")]
    public class RenderColour : GH_Component
    {
        public RenderColour()
          : base("Render Colour", "Colour",
              "Adds render colour to robot mesh",
              "Robots", "Utility")
        { }
        public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure | GH_Exposure.hidden;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        { }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Archive archive = new GH_Archive();
            byte[] binaryRep = Convert.FromBase64String(snip);
            archive.Deserialize_Binary(binaryRep);
            string xmlSnippet = archive.Serialize_Xml();
            Clipboard.Clear();
            Clipboard.SetText(xmlSnippet);
            Paste();
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10, DeleteThis);
        }

        readonly string snip = "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";

        void DeleteThis(GH_Document doc)
        {
            Grasshopper.Instances.ActiveCanvas.Document.RemoveObject(this, false);
        }
        void Paste()
        {
            GH_DocumentIO documentIO = new GH_DocumentIO();
            documentIO.Paste(GH_ClipboardType.System);
            var thispivot = Attributes.Pivot;

            int smallestX = int.MaxValue;
            int smallestY = int.MaxValue;
            foreach (IGH_DocumentObject obj in documentIO.Document.Objects)
            {
                var pivot = obj.Attributes.Pivot;
                if (pivot.X < smallestX) smallestX = (int)pivot.X;
                if (pivot.Y < smallestY) smallestY = (int)pivot.Y;
            }

            Size offset = new Size((int)thispivot.X - smallestX, (int)thispivot.Y - smallestY);

            documentIO.Document.TranslateObjects(offset, false);
            documentIO.Document.SelectAll();
            documentIO.Document.ExpireSolution();
            documentIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = documentIO.Document.Objects;
            Grasshopper.Instances.ActiveCanvas.Document.DeselectAll();
            Grasshopper.Instances.ActiveCanvas.Document.MergeDocument(documentIO.Document);
            Grasshopper.Instances.ActiveCanvas.Document.UndoUtil.RecordAddObjectEvent("Paste", objs);
            Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(10);
        }
        protected override Bitmap Icon => Properties.Resources.Colourful;
        public override Guid ComponentGuid => new Guid("5AE1E121-11B3-499A-AB30-82B02FAD533A");
    }

}