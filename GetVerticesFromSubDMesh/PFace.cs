using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
#endregion

[assembly: CommandClass(typeof(GetVerticesFromSubDMesh.PFace))]

namespace GetVerticesFromSubDMesh
{
    public class PFace
    {

        [CommandMethod("xPface")]
        public void Select()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Subclass, "AcDbPolyFaceMesh") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "Select Polyface Mesh object: ";
                PromptSelectionResult res = ed.GetSelection(opts, filter);

                if (res.Status != PromptStatus.OK)
                    return;

                SelectionSet selSet = res.Value;
                ObjectId[] ids = selSet.GetObjectIds();

                tr.Commit();
            }
        }
    }
}
