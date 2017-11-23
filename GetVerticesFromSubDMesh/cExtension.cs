using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(GetVerticesFromSubDMesh.GetVerticesFromSubDMesh))]

namespace GetVerticesFromSubDMesh
{
    public class GetVerticesFromSubDMesh : IExtensionApplication
    {
        [CommandMethod("info")]
        public void Initialize()
        {
            Active.Editor.WriteMessage("\n-> Get vertex points from MeshObject: xMeshPoints");
            Active.Editor.WriteMessage("\n-> Get enclosed Polyline from Point cloud: xEnclosePoints_Polyline");
            Active.Editor.WriteMessage("\n-> Get enclosed Polyline as Rectangle from Point cloud: xEnclosePoints_Rectangle");
            Active.Editor.WriteMessage("\n-> Create Civil3d Tinsurface from Mesh: xTinsurfaceFromMesh");
        }

        public void Terminate()
        {
        }
    }
}
