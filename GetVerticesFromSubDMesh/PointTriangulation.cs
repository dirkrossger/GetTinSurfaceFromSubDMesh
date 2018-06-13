using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region Autodesk
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
#endregion

[assembly: CommandClass(typeof(PointTriangulation.CommandMethods))]


namespace PointTriangulation
{
    class Triangulation
    {
        private int npts, ntri, nouted;
        private int[] pt1, pt2, pt3, ed1, ed2, outed1;
        private double[] ptx, pty, ptz;
        private bool createSolid;
        private double zref;

        public Triangulation(ObjectId[] ids, bool createSolid, double zref)
        {
            this.createSolid = createSolid;
            this.zref = zref;
            Triangulate(ids);
            Count = ntri;
        }

        public Triangulation(ObjectId[] ids)
            : this(ids, false, 0.0) { }

        public int Count { get; private set; }

        public int MakeFaces()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                for (int i = 0; i < ntri; i++)
                {
                    using (Face face = new Face(
                        new Point3d(ptx[pt1[i]], pty[pt1[i]], ptz[pt1[i]]),
                        new Point3d(ptx[pt2[i]], pty[pt2[i]], ptz[pt2[i]]),
                        new Point3d(ptx[pt3[i]], pty[pt3[i]], ptz[pt3[i]]),
                        true, true, true, true))
                    {
                        btr.AppendEntity(face);
                        tr.AddNewlyCreatedDBObject(face, true);
                    }
                }
                tr.Commit();
            }
            return ntri;
        }

        public void MakePolyFaceMesh()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                using (PolyFaceMesh pfm = new PolyFaceMesh())
                {
                    btr.AppendEntity(pfm);
                    tr.AddNewlyCreatedDBObject(pfm, true);
                    for (int i = 0; i < npts; i++)
                    {
                        using (PolyFaceMeshVertex vert =
                            new PolyFaceMeshVertex(new Point3d(ptx[i], pty[i], ptz[i])))
                        {
                            pfm.AppendVertex(vert);
                        }
                    }
                    for (int i = 0; i < ntri; i++)
                    {
                        using (FaceRecord face =
                            new FaceRecord((short)(pt1[i] + 1), (short)(pt2[i] + 1), (short)(pt3[i] + 1), 0))
                        {
                            pfm.AppendFaceRecord(face);
                        }
                    }
                }
                tr.Commit();
            }
        }

        public void MakeSubDMesh()
        {
            Point3dCollection vertices = new Point3dCollection();
            Int32Collection faces = new Int32Collection();

            for (int i = 0; i < npts; i++)
                vertices.Add(new Point3d(ptx[i], pty[i], ptz[i]));

            for (int i = 0; i < ntri; i++)
            {
                faces.Add(3);
                faces.Add(pt1[i]);
                faces.Add(pt2[i]);
                faces.Add(pt3[i]);
            }

            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                using (SubDMesh sdm = new SubDMesh())
                {
                    sdm.SetDatabaseDefaults();
                    sdm.SetSubDMesh(vertices, faces, 0);
                    btr.AppendEntity(sdm);
                    tr.AddNewlyCreatedDBObject(sdm, true);
                }
                tr.Commit();
            }
        }

        public void MakeSolid3d()
        {
            Point3dCollection vertices = new Point3dCollection();
            Int32Collection faces = new Int32Collection();

            for (int i = 0; i < npts; i++)
                vertices.Add(new Point3d(ptx[i], pty[i], ptz[i]));

            for (int i = 0; i < nouted; i++)
                vertices.Add(new Point3d(ptx[ed1[outed1[i]]], pty[ed1[outed1[i]]], zref));

            for (int i = 0; i < ntri; i++)
            {
                faces.Add(3);
                faces.Add(pt1[i]);
                faces.Add(pt2[i]);
                faces.Add(pt3[i]);
            }

            for (int i = 0; i < nouted; i++)
            {
                faces.Add(4);
                int k = outed1[i];
                faces.Add(ed1[k]);
                faces.Add(ed2[k]);
                if (i == nouted - 1)
                {
                    faces.Add(npts);
                }
                else
                {
                    faces.Add(npts + i + 1);
                }
                faces.Add(npts + i);
            }
            faces.Add(nouted);
            for (int i = 0; i < nouted; i++)
                faces.Add(npts + i);
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                using (SubDMesh sdm = new SubDMesh())
                {
                    sdm.SetDatabaseDefaults();
                    sdm.SetSubDMesh(vertices, faces, 0);
                    btr.AppendEntity(sdm);
                    tr.AddNewlyCreatedDBObject(sdm, true);
                    try
                    {
                        using (Solid3d sol = sdm.ConvertToSolid(false, false))
                        {
                            btr.AppendEntity(sol);
                            tr.AddNewlyCreatedDBObject(sol, true);
                        }
                    }
                    catch
                    {
                        AcAp.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                            "\nMesh was too complex to turn into a solid.");
                    }
                    sdm.Erase();
                }
                tr.Commit();
            }
        }

        private bool circum(double x1, double y1, double x2, double y2, double x3, double y3, ref double xc, ref double yc, ref double r)
        {
            const double eps = 1e-6;
            const double big = 1e12;
            bool result = true;
            double m1, m2, mx1, mx2, my1, my2, dx, dy;

            if ((Math.Abs(y1 - y2) < eps) && (Math.Abs(y2 - y3) < eps))
            {
                result = false;
                xc = x1; yc = y1; r = big;
            }
            else
            {
                if (Math.Abs(y2 - y1) < eps)
                {
                    m2 = -(x3 - x2) / (y3 - y2);
                    mx2 = (x2 + x3) / 2;
                    my2 = (y2 + y3) / 2;
                    xc = (x2 + x1) / 2;
                    yc = m2 * (xc - mx2) + my2;
                }
                else if (Math.Abs(y3 - y2) < eps)
                {
                    m1 = -(x2 - x1) / (y2 - y1);
                    mx1 = (x1 + x2) / 2;
                    my1 = (y1 + y2) / 2;
                    xc = (x3 + x2) / 2;
                    yc = m1 * (xc - mx1) + my1;
                }
                else
                {
                    m1 = -(x2 - x1) / (y2 - y1);
                    m2 = -(x3 - x2) / (y3 - y2);
                    if (Math.Abs(m1 - m2) < eps)
                    {
                        result = false;
                        xc = x1;
                        yc = y1;
                        r = big;
                    }
                    else
                    {
                        mx1 = (x1 + x2) / 2;
                        mx2 = (x2 + x3) / 2;
                        my1 = (y1 + y2) / 2;
                        my2 = (y2 + y3) / 2;
                        xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
                        yc = m1 * (xc - mx1) + my1;
                    }
                }
            }
            dx = x2 - xc;
            dy = y2 - yc;
            r = dx * dx + dy * dy;
            return result;
        }

        private void Triangulate(ObjectId[] ids)
        {
            Point3d[] pts = new Point3d[ids.Length];
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                pts = ids
                    .Select(id => ((DBPoint)tr.GetObject(id, OpenMode.ForRead)).Position)
                    .Distinct()
                    .ToArray();
            }

            npts = ids.Length;

            if (npts < 3)
                throw new ArgumentException("Minimum of 3 points must be selected!");

            ProgressMeter progress = new ProgressMeter();
            progress.SetLimit(npts);
            progress.Start("Triangulation...");
            progress.SetLimit(npts);

            int i, j, k, ned, status1 = 0;
            bool status;

            // Point coordinates
            ptx = new double[npts + 3];
            pty = new double[npts + 3];
            ptz = new double[npts + 3];

            // Triangle definitions
            pt1 = new int[npts * 2 + 1];
            pt2 = new int[npts * 2 + 1];
            pt3 = new int[npts * 2 + 1];

            // Circumscribed circle
            double[] cex = new double[npts * 2 + 1];
            double[] cey = new double[npts * 2 + 1];
            double[] rad = new double[npts * 2 + 1];
            double xmin, ymin, xmax, ymax, dx, dy, xmid, ymid, dmax;
            ed1 = new int[npts * 2 + 1];
            ed2 = new int[npts * 2 + 1];
            if (createSolid)
                outed1 = new int[npts + 1];

            k = 0;
            for (i = 0; i < npts; i++)
            {
                Point3d pt = pts[i];
                ptx[i] = pt.X;
                pty[i] = pt.Y;
                ptz[i] = pt.Z;
                k++;
            }

            // Supertriangle
            xmin = ptx[0]; xmax = xmin;
            ymin = pty[0]; ymax = ymin;
            for (i = 0; i < npts; i++)
            {
                if (ptx[i] < xmin) xmin = ptx[i];
                if (ptx[i] > xmax) xmax = ptx[i];
                if (pty[i] < xmin) ymin = pty[i];
                if (pty[i] > xmin) ymax = pty[i];
            }

            dx = xmax - xmin;
            dy = ymax - ymin;
            dmax = dx < dy ? dy : dx;
            xmid = (xmin + xmax) / 2; ymid = (ymin + ymax) / 2;
            i = npts;
            ptx[i] = xmid - 2 * dmax;
            pty[i] = ymid - dmax;
            ptz[i] = 0;
            pt1[0] = i;
            i++;
            ptx[i] = xmid + 2 * dmax;
            pty[i] = ymid - dmax;
            ptz[i] = 0;
            pt2[0] = i;
            i++;
            ptx[i] = xmid;
            pty[i] = ymid + 2 * dmax;
            ptz[i] = 0;
            pt3[0] = i;
            ntri = 1;
            circum(
                ptx[pt1[0]], pty[pt1[0]], ptx[pt2[0]],
                pty[pt2[0]], ptx[pt3[0]], pty[pt3[0]],
                ref cex[0], ref cey[0], ref rad[0]
                );

            // Main loop
            for (i = 0; i < npts; i++)
            {
                progress.MeterProgress();

                ned = 0;
                xmin = ptx[i]; ymin = pty[i];
                j = 0;
                while (j < ntri)
                {
                    dx = cex[j] - xmin; dy = cey[j] - ymin;
                    if (((dx * dx) + (dy * dy)) < rad[j])
                    {
                        ed1[ned] = pt1[j]; ed2[ned] = pt2[j];
                        ned++;
                        ed1[ned] = pt2[j]; ed2[ned] = pt3[j];
                        ned++;
                        ed1[ned] = pt3[j]; ed2[ned] = pt1[j];
                        ned++;
                        ntri--;
                        pt1[j] = pt1[ntri];
                        pt2[j] = pt2[ntri];
                        pt3[j] = pt3[ntri];
                        cex[j] = cex[ntri];
                        cey[j] = cey[ntri];
                        rad[j] = rad[ntri];
                        j--;
                    }
                    j++;
                }

                for (j = 0; j < ned - 1; j++)
                    for (k = j + 1; k < ned; k++)
                        if ((ed1[j] == ed2[k]) && (ed2[j] == ed1[k]))
                        {
                            ed1[j] = -1;
                            ed2[j] = -1;
                            ed1[k] = -1;
                            ed2[k] = -1;
                        }

                for (j = 0; j < ned; j++)
                    if ((ed1[j] >= 0) && (ed2[j] >= 0))
                    {
                        pt1[ntri] = ed1[j];
                        pt2[ntri] = ed2[j];
                        pt3[ntri] = i;
                        status = circum(
                            ptx[pt1[ntri]], pty[pt1[ntri]], ptx[pt2[ntri]],
                            pty[pt2[ntri]], ptx[pt3[ntri]], pty[pt3[ntri]],
                            ref cex[ntri], ref cey[ntri], ref rad[ntri]
                            );
                        if (!status)
                        {
                            status1++;
                        }
                        ntri++;
                    }
            }

            // Removal of outer triangles
            i = 0;
            nouted = 0;
            while (i < ntri)
            {
                if ((pt1[i] >= npts) || (pt2[i] >= npts) || (pt3[i] >= npts))
                {
                    if (createSolid)
                    {
                        if ((pt1[i] >= npts) && (pt2[i] < npts) && (pt3[i] < npts))
                        {
                            ed1[nouted] = pt2[i];
                            ed2[nouted] = pt3[i];
                            nouted++;
                        }
                        if ((pt2[i] >= npts) && (pt1[i] < npts) && (pt3[i] < npts))
                        {
                            ed1[nouted] = pt3[i];
                            ed2[nouted] = pt1[i];
                            nouted++;
                        }
                        if ((pt3[i] >= npts) && (pt1[i] < npts) && (pt2[i] < npts))
                        {
                            ed1[nouted] = pt1[i];
                            ed2[nouted] = pt2[i];
                            nouted++;
                        }
                    }
                    ntri--;
                    pt1[i] = pt1[ntri];
                    pt2[i] = pt2[ntri];
                    pt3[i] = pt3[ntri];
                    cex[i] = cex[ntri];
                    cey[i] = cey[ntri];
                    rad[i] = rad[ntri];
                    i--;
                }
                i++;
            }

            if (createSolid)
            {
                outed1[0] = 0;
                for (i = 1; i < nouted; i++)
                    for (j = 1; j < nouted; j++)
                        if (ed2[outed1[i - 1]] == ed1[j])
                        {
                            outed1[i] = j;
                            j = nouted;
                        }
                outed1[nouted] = 0;
            }

            progress.Stop();
        }
    }

    public class CommandMethods
    {
        Editor ed = AcAp.DocumentManager.MdiActiveDocument.Editor;

        [CommandMethod("TRIANG_FACE3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ToFace3d()
        {
            ObjectId[] ids = SelectPoints();
            if (ids == null)
                return;

            try
            {
                Triangulation triangles = new Triangulation(ids);
                triangles.MakeFaces();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }
        }

        [CommandMethod("TRIANG_POLYFACE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ToPolyFaceMesh()
        {
            ObjectId[] ids = SelectPoints();
            if (ids == null)
                return;

            if (ids.Length > 32767)
            {
                AcAp.ShowAlertDialog("Maximum number of points (32767) exceeded!");
                return;
            }

            try
            {
                Triangulation triangles = new Triangulation(ids);
                triangles.MakePolyFaceMesh();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }
        }

        [CommandMethod("TRIANG_SUBDMESH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ToSubDMesh()
        {
            ObjectId[] ids = SelectPoints();
            if (ids == null)
                return;

            try
            {
                Triangulation triangles = new Triangulation(ids);
                triangles.MakeSubDMesh();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }
        }

        [CommandMethod("TRIANG_SOLID3D", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ToSolid3d()
        {
            ObjectId[] ids = SelectPoints();
            if (ids == null)
                return;

            PromptDoubleOptions pdo = new PromptDoubleOptions(
                "\nEnter Z coordinate of reference plane: ");
            pdo.AllowZero = true;
            pdo.DefaultValue = 0.0;
            pdo.UseDefaultValue = true;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return;

            try
            {
                Triangulation triangles = new Triangulation(ids, true, pdr.Value);
                triangles.MakeSolid3d();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }
        }

        private ObjectId[] SelectPoints()
        {
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSelect points (or Enter for all): ";
            SelectionFilter filter = new SelectionFilter(
                new TypedValue[2] { new TypedValue(0, "POINT"), new TypedValue(410, "Model") });
            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if (psr.Status == PromptStatus.Error)
                psr = ed.SelectAll(filter);
            if (psr.Status == PromptStatus.OK)
                return psr.Value.GetObjectIds();
            return null;
        }
    }
}

