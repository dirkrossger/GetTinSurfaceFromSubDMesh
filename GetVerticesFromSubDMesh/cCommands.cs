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
        [CommandMethod("xMeshPoints")]
        public void GetPointsMesh()
        {
            Point3dCollection coll3d = cMesh.GetSubDMeshVertices();
            foreach (Point3d p3 in coll3d)
            {
                cPoint.AddPoint(p3);
            }
        }

        [CommandMethod("xEnclosePoints_Polyline")]
        public void EnclosePoints_Polyline()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            cPoint oPoint = new cPoint();
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
                pts = oPoint.ConvexHull(pts);
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

        [CommandMethod("xEnclosePoints_Rectangle")]
        public void EnclosePoints_Rectangle()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = doc.Database;
            Editor ed = doc.Editor;

            Point3dCollection pts = cPoint.GetPoints();
            CoordinateSystem3d ucs = ed.CurrentUserCoordinateSystem.CoordinateSystem3d;
            //object bufvar = Application.GetSystemVariable("ENCLOSINGBOUNDARYBUFFER");
            //if (bufvar != null)
            //{
            //    short bufval = (short)bufvar;
            //    double buffer = bufval / 100.0;
            //    cPoint.RectangleFromPoints(pts, ucs, buffer);
            //}

            try
            {
                Entity rectang = cPoint.RectangleFromPoints(pts, ucs);

                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    using (Polyline2d poly = new Polyline2d())
                    {
                        // Add the new object to the block table record and the transaction
                        acBlkTblRec.AppendEntity(rectang);
                        acTrans.AddNewlyCreatedDBObject(rectang, true);
                    }

                    // Save the new object to the database
                    acTrans.Commit();
                }
            }
            catch (System.Exception ex) { }
        }

        [CommandMethod("xTinsurfaceFromMesh")]
        public void TinsurfaceFromMesh()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            try
            {
                List<MeshDatas> list = cMesh.GetMeshDatas();

                #region Create Boundary from SubDMesh object
                // Create Boundaries from SubDMesh object
                foreach (MeshDatas x in list)
                {
                    cEntity.ObjectsToEnclose(x.Mesh as Entity);
                }
                acDoc.SendStringToExecute(" ", true, false, false);
                #endregion

                // Create Surface from SubDMesh object and Add Vertices
                cTinSurface oTinsurf = new cTinSurface();
                oTinsurf.Create("Test", "Trianglar Punkter och gräns", "Created from SubDMesh"); // "Nivåkurvor och gräns"
                oTinsurf.AddPointsToSurface();

                // Create Borderline from Surface
                oTinsurf.GetBorderFromSurface();

                // Add Borderline to Surface and Hide ...
                oTinsurf.AddBoundaryToSurfaceHide();
            }
            catch(System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Can´t create TinSurface, error:" + ex);
            }

        }
    }
}
