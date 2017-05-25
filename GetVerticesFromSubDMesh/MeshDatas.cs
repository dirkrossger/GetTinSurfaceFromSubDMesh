using System;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
#endregion

namespace GetVerticesFromSubDMesh
{
    class MeshDatas
    {
        public int Increment { get; set; }
        public Point3dCollection Points { get; set; }
    }
}
