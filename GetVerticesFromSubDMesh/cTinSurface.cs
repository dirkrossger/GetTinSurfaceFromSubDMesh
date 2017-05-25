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

using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
#endregion

namespace GetVerticesFromSubDMesh
{
    class cTinSurface
    {
        public cTinSurface() { }

        private TinSurface ts;
        private Document acadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

        public void CreateTinSurface(string m_Name, string m_Stylename, string m_Description)
        {
            Editor ed = acadDoc.Editor;
            ObjectId m_SurfaceId = ObjectId.Null;

            using (Transaction tr = acadDoc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    CivilDocument civDoc = CivilApplication.ActiveDocument;
                    ObjectId objStyl = civDoc.Styles.SurfaceStyles[m_Stylename];

                    m_SurfaceId = TinSurface.Create(m_Name, objStyl);
                    ts = m_SurfaceId.GetObject(OpenMode.ForRead) as TinSurface;
                    ts.Description = m_Description;
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\nError: Missing Surfacestyle!");
                }
                finally
                {
                    tr.Commit();
                }
            }


        }

        public void AddPointsToSurface()
        {
            Editor ed = acadDoc.Editor;

            try
            {
                Point3dCollection coll3d = cMesh.GetSubDMeshVertices();
                ts.AddVertices(coll3d);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError: Can´t add Vertices from SubDMesh!");
            }
        }
    }
}
