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

[assembly: CommandClass(typeof(GetVerticesFromSubDMesh.Commands))]

namespace GetVerticesFromSubDMesh
{
    public class Commands
    {
        [CommandMethod("xPoints")]
        public void GetPointsMesh()
        {
            Point3dCollection coll3d = cMesh.GetSubDMeshVertices();
            foreach (Point3d p3 in coll3d)
            {
                cPoint.AddPoint(p3);
            }
        }

        [CommandMethod("xPolyline")]
        public void Enclose()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            cEntity oEnt = new cEntity();
            TypedValue[] filter = new TypedValue[1] { new TypedValue(0, "POINT") };
            PromptSelectionResult psr = ed.GetSelection(new SelectionFilter(filter));
            if (psr.Status != PromptStatus.OK) return;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (Polyline pline = new Polyline())
            {
                List<Point2d> pts = new List<Point2d>();
                foreach (SelectedObject so in psr.Value)
                {
                    DBPoint dbPt = (DBPoint)tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    pts.Add(new Point2d(dbPt.Position.X, dbPt.Position.Y));
                }
                pts = oEnt.ConvexHull(pts);
                for (int i = 0; i < pts.Count; i++)
                {
                    pline.AddVertexAt(i, pts[i], 0.0, 0.0, 0.0);
                }
                pline.Closed = true;
                pline.SetDatabaseDefaults();
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
                tr.Commit();
            }
        }
    }

}
