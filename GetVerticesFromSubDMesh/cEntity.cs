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
    }
}
