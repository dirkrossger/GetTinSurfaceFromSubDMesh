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
        public static void SelectObjectsToEnclose()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();

                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    SelectionSet acSSet = acSSPrompt.Value;

                    foreach (SelectedObject acSSObj in acSSet)
                    {
                        // Check to make sure a valid SelectedObject object was returned
                        if (acSSObj != null)
                        {
                            // Open the selected object for write
                            Entity acEnt = acTrans.GetObject(acSSObj.ObjectId, OpenMode.ForRead) as Entity;

                            if (acEnt != null)
                            {
                                string strHandle = acEnt.Handle.ToString();
                                string command = (string.Format("(handent \"" + strHandle + "\")  "));
                                acDoc.SendStringToExecute("lineworkshrinkwrap ", true, false, false);
                                acDoc.SendStringToExecute(command, true, false, false);
                            }
                        }
                    }
                }
            }
        }

        public static void ObjectsToEnclose(Entity acEnt)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            string strHandle = acEnt.Handle.ToString();
            acDoc.SendStringToExecute("lineworkshrinkwrap ", true, false, false);

            string command = string.Format("(handent \"" + strHandle + "\") ");
            acDoc.SendStringToExecute(command, true, false, false);
        }
    }
}
