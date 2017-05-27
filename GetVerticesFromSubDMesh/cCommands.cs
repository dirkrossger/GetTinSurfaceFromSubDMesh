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
        private ObjectIdCollection PolylineColl;

        [CommandMethod("xx")]
        public void Start()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            List<MeshDatas> list = cMesh.GetMeshDatas();

            // Create Boundaries from SubDMesh object
            foreach (MeshDatas x in list)
            {
                cEntity.ObjectsToEnclose(x.Mesh as Entity);
            }
            acDoc.SendStringToExecute(" ", true, false, false);

            // Create Surface from SubDMesh object and Add Vertices
            cTinSurface oTinsurf = new cTinSurface();
            oTinsurf.CreateTinSurface("Test", "Trianglar Punkter och gräns", "Created from SubDMesh"); // "Nivåkurvor och gräns"
            oTinsurf.AddPointsToSurface();

            // Create Borderline from Surface
            oTinsurf.GetBorderFromSurface();

            // Add Borderline to Surface and Hide ...
            oTinsurf.AddBoundaryToSurfaceHide();

            #region Collect all creates 2dPolylines (Boundaries)
            if (PolylineColl == null)
            {
                PolylineColl = new ObjectIdCollection();
            }
            ObjectId id = cEntity.GetLastEntity();
            cEntity.CurrentlySelected();
            #endregion
        }
    }



    //#region Create Points on vertices from SubDMesh
    //public void Start()
    //{
    //    Point3dCollection coll3d = cMesh.GetSubDMeshVertices();
    //    foreach(Point3d p3 in coll3d)
    //    {
    //        cPoint.AddPoint(p3);
    //    }
    //}
    //#endregion
}
