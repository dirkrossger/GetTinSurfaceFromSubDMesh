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

[assembly: CommandClass(typeof(GetVerticesFromSubDMesh.PFace))]

namespace GetVerticesFromSubDMesh
{
    public class PFace
    {
        private ObjectId[] pFaceIds;

        [CommandMethod("xPface")]
        public void Select()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Subclass, "AcDbPolyFaceMesh") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "Select Polyface Mesh object: ";
                PromptSelectionResult res = ed.GetSelection(opts, filter);

                if (res.Status != PromptStatus.OK)
                    return;

                SelectionSet selSet = res.Value;
                pFaceIds = selSet.GetObjectIds();
                TriangulatePolyfaceMeshWithLines();

                tr.Commit();
            }
        }

        public void TriangulatePolyfaceMeshWithLines()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            int color = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                if (pFaceIds.Count() > 0)
                {
                    for (int incr = 0; incr < pFaceIds.Count(); incr++)
                    {
                        PolyFaceMesh pfm = tr.GetObject(pFaceIds[incr], OpenMode.ForRead) as PolyFaceMesh;

                        //ed.WriteMessage(String.Format("\n Vertices : {0}", pfm.NumVertices));
                        //ed.WriteMessage(String.Format("\n Faces    : {0}", pfm.NumFaces));

                        Point3dCollection vertices = new Point3dCollection();
                        foreach (ObjectId id in pfm)
                        {
                            DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                            if (obj is PolyFaceMeshVertex)
                            {
                                PolyFaceMeshVertex vertex = (PolyFaceMeshVertex)obj;
                                vertices.Add(vertex.Position);
                            }
                            else if (obj is FaceRecord)
                            {
                                FaceRecord face = (FaceRecord)obj;
                                Point3dCollection pts = new Point3dCollection();

                                for (short i = 0; i < 4; i++)
                                {
                                    short index = face.GetVertexAt(i);
                                    if (index != 0)
                                        pts.Add(vertices[Math.Abs(index) - 1]);
                                }
                                // If there are 4 points then we draw crosses
                                // (could also be 3)

                                if (pts.Count == 4)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        Line line = new Line(pts[j], pts[j + 2]);
                                        line.ColorIndex = color;
                                        ms.AppendEntity(line);
                                        tr.AddNewlyCreatedDBObject(line, true);
                                    }
                                }
                                color = (color + 1) % 7;
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}