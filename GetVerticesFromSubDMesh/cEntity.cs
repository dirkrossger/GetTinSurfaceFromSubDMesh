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

namespace GetVerticesFromSubDMesh
{
    class cEntity
    {
        public cEntity(){}
        public static void ObjectsToEnclose(Entity acEnt)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            string strHandle = acEnt.Handle.ToString();
            acDoc.SendStringToExecute("lineworkshrinkwrap ", true, false, false);

            string command = string.Format("(handent \"" + strHandle + "\") ");
            acDoc.SendStringToExecute(command, true, false, false);
        }

        public static ObjectId GetLastEntity()
        {
            return Autodesk.AutoCAD.Internal.Utils.EntLast();
        }

        public static void CurrentlySelected()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            PromptSelectionResult selectionResult = acDoc.Editor.SelectImplied();

            if (selectionResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    SelectionSet currentlySelectedEntities = selectionResult.Value;
                    foreach (ObjectId id in currentlySelectedEntities.GetObjectIds())
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        acDoc.Editor.WriteMessage("\n..." + ent.ToString());
                    }
                }
            }
            else
                acDoc.Editor.WriteMessage("\n...SelectionResult.Status=" + selectionResult.Status.ToString());
        }

        #region Collect Entity from exploded Block
        static ObjectIdCollection ids = new ObjectIdCollection();
        public static void ExplodeToOwnerSpace()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions options = new PromptEntityOptions("\nSelect block reference");
            options.SetRejectMessage("\nSelect only block reference");
            options.AddAllowedClass(typeof(BlockReference), false);

            PromptEntityResult acSSPrompt = ed.GetEntity(options);

            using (Transaction tx = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tx.GetObject(acSSPrompt.ObjectId, OpenMode.ForRead) as BlockReference;
                //add event
                ids.Clear();
                db.ObjectAppended += new ObjectEventHandler(db_ObjectAppended);
                blockRef.ExplodeToOwnerSpace();
                //remove event
                db.ObjectAppended -= new ObjectEventHandler(db_ObjectAppended);

                foreach (ObjectId id in ids)
                {
                    //get each entity....
                    ed.WriteMessage("\n" + id.ToString());
                }

                tx.Commit();
            }
        }

        static void db_ObjectAppended(object sender, ObjectEventArgs e)
        {
            //add the object id
            ids.Add(e.DBObject.ObjectId);
        }
        #endregion

        #region Create Polyline from DbPoints
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
    }
}
