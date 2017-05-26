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
        [CommandMethod("xx")]

        #region Create Boundary from SubDMesh object
        public void Start()
        {
            List<MeshDatas> list = cMesh.GetMeshBlocksVertices();
            foreach(MeshDatas x in list)
            {
                cMesh.GetMeshBoundary(x.Points);
            }
        }
        #endregion

        //#region Create Surface from SubDMesh object
        //public void Start()
        //{
        //    cTinSurface oTinsurf = new cTinSurface();
        //    oTinsurf.CreateTinSurface("Test", "Trianglar Punkter och gräns", "Created from SubDMesh"); // "Nivåkurvor och gräns"
        //    oTinsurf.AddPointsToSurface();
        //}
        //#endregion

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
}
