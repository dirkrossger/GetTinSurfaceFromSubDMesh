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
        private ObjectId border;
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

        public void GetBorderFromSurface()
        {
            ObjectIdCollection entityIds = ts.ExtractBorder(Autodesk.Civil.SurfaceExtractionSettingsType.Model);
            border = ObjectId.Null;

            try
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    ObjectId entityId = entityIds[i];
                    if (entityId.ObjectClass == RXClass.GetClass(typeof(Polyline3d)))
                    {
                        border = entityId; //entityId.GetObject(OpenMode.ForRead) as Polyline3d;
                    }
                }
            }
            catch(System.Exception ex)
            { }
        }

        public void AddBoundaryToSurfaceHide()
        {
            Editor ed = acadDoc.Editor;
            ObjectId[] boundaries = { border };

            try
            {
                ts.BoundariesDefinition.AddBoundaries(
                    new ObjectIdCollection(boundaries), 100, Autodesk.Civil.SurfaceBoundaryType.Hide, true);
                ts.Rebuild();
            }

            catch (System.Exception e)
            {
                ed.WriteMessage("Failed to add the boundary: {0}", e.Message);
            }
        }

        public void AddBoundaryToSurfaceShow()
        {

        }
    }
}
