#region System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
#endregion

namespace GetVerticesFromSubDMesh
{
    class cPoint
    {
        public static void AddPoint(Point3d pt3)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                using (DBPoint acPoint = new DBPoint(pt3))
                {
                    // Add the new object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acPoint);
                    acTrans.AddNewlyCreatedDBObject(acPoint, true);
                }

                // Save the new object to the database
                acTrans.Commit();
            }
        }

        public static Point3dCollection GetPoints()
        {
            Document activeDoc = Application.DocumentManager.MdiActiveDocument;
            Database db = activeDoc.Database;
            Editor ed = activeDoc.Editor;

            TypedValue[] values = { new TypedValue((int)DxfCode.Start, "Point") };

            SelectionFilter filter = new SelectionFilter(values);
            PromptSelectionResult psr = ed.SelectAll(filter);
            SelectionSet ss = psr.Value;
            if (ss == null)
                return null;

            Point3dCollection collPoints = new Point3dCollection();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < ss.Count; ++i)
                {
                    DBPoint dbPt = trans.GetObject(ss[i].ObjectId, OpenMode.ForRead) as DBPoint;
                    collPoints.Add(new Point3d(dbPt.Position.X, dbPt.Position.Y, dbPt.Position.Z));
                }
                trans.Commit();
            }
            return collPoints;
        }

        public static double Distance(Point3d p1, Point3d p2)
        {
            return p1.GetVectorTo(p2).Length;
        }

        #region Create enclosed Polyline from DbPoints
        private Point2d _p0;

        private bool Clockwise(Point2d p1, Point2d p2, Point2d p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-9;
        }

        private double Cosine(Point2d pt)
        {
            double d = _p0.GetDistanceTo(pt);
            return d == 0.0 ? 1.0 : Math.Round((pt.X - _p0.X) / d, 9);
        }

        public List<Point2d> ConvexHull(List<Point2d> pts)
        {
            _p0 = pts.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            pts = pts.OrderByDescending(p => Cosine(p)).ThenBy(p => _p0.GetDistanceTo(p)).ToList();
            for (int i = 1; i < pts.Count - 1; i++)
            {
                while (i > 0 && Clockwise(pts[i - 1], pts[i], pts[i + 1]))
                {
                    pts.RemoveAt(i);
                    i--;
                }
            }
            return pts;
        }
        #endregion

        #region Enclose DbPoints with Rectangle
        public static Entity RectangleFromPoints(Point3dCollection pts, CoordinateSystem3d ucs)
        {
            // Get the plane of the UCS

            Plane pl = new Plane(ucs.Origin, ucs.Zaxis);

            // We will project these (possibly 3D) points onto
            // the plane of the current UCS, as that's where
            // we will create our circle

            // Project the points onto it

            List<Point2d> pts2d = new List<Point2d>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
            {
                pts2d.Add(pl.ParameterOf(pts[i]));
            }

            // Assuming we have some points in our list...

            if (pts.Count > 0)
            {
                // Set the initial min and max values from the first entry

                double minX = pts2d[0].X,
                       maxX = minX,
                       minY = pts2d[0].Y,
                       maxY = minY;

                // Perform a single iteration to extract the min/max X and Y

                for (int i = 1; i < pts2d.Count; i++)
                {
                    Point2d pt = pts2d[i];
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

                // Our final buffer amount will be the percentage of the
                // smallest of the dimensions

                double buf =
                  Math.Min(maxX - minX, maxY - minY);

                // Apply the buffer to our point ordinates

                minX -= buf;
                minY -= buf;
                maxX += buf;
                maxY += buf;

                // Create the boundary points

                Point2d pt0 = new Point2d(minX, minY),
                        pt1 = new Point2d(minX, maxY),
                        pt2 = new Point2d(maxX, maxY),
                        pt3 = new Point2d(maxX, minY);

                // Finally we create the polyline

                var p = new Polyline(4);
                p.Normal = pl.Normal;
                p.AddVertexAt(0, pt0, 0, 0, 0);
                p.AddVertexAt(1, pt1, 0, 0, 0);
                p.AddVertexAt(2, pt2, 0, 0, 0);
                p.AddVertexAt(3, pt3, 0, 0, 0);
                p.Closed = true;

                return p;
            }
            return null;
        }
        #endregion
    }
}
