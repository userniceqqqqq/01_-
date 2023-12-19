using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    public class LoadFamilyTreeSource : IExternalEventHandler
    {
        public FamilySource LoadSource { get; set; } = FamilySource.Document;
        public IList<Element> FamilyList { get; set; } = new List<Element>();

        public void Execute(UIApplication app)
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;
            FamilyList.Clear();

            if (LoadSource != FamilySource.Selection)
            {
                FilteredElementCollector collector;
                if (LoadSource == FamilySource.Document)//文档中族
                {
                    collector = new FilteredElementCollector(doc);
                    IList<Element> listFamily = collector.OfClass(typeof(Family)).ToElements();

                    foreach (var element in listFamily)
                    {
                        Family curFamily = element as Family;
                        if (curFamily.IsInPlace || !curFamily.IsEditable)//排除内建族和不可编辑族——调用Document.EditFamily(family)时会抛出family，无法获取族文档
                        {
                            continue;
                        }
                        foreach (var familySymbolId in curFamily.GetFamilySymbolIds())
                        {
                            Element curFamilySymbolElement = doc.GetElement(familySymbolId);
                            FamilySymbol curFamilySymbol = curFamilySymbolElement as FamilySymbol;
                            if (curFamilySymbol.Category == null)
                            {
                                continue;
                            }
                            if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !FamilyList.Contains(curFamilySymbolElement))
                            {
                                FamilyList.Add(curFamilySymbolElement);
                            }
                        }
                    }
                }
                else//当前活动视图中族
                {
                    ElementId viewId = doc.ActiveView.Id;
                    collector = new FilteredElementCollector(doc, viewId);
                    IList<ElementId> familySymbolEleId = new List<ElementId>();
                    IList<Element> listFamilyInstance = collector.OfClass(typeof(FamilyInstance)).ToElements();

                    foreach (var element in listFamilyInstance)
                    {
                        FamilyInstance curFamilyInstance = element as FamilyInstance;
                        FamilySymbol curFamilySymbol = curFamilyInstance.Symbol;
                        if (curFamilySymbol.Family.IsInPlace || !curFamilySymbol.Family.IsEditable)
                        {
                            continue;
                        }

                        if (curFamilySymbol.Category == null)
                        {
                            continue;
                        }
                        if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familySymbolEleId.Contains(curFamilySymbol.Id))
                        {
                            familySymbolEleId.Add(curFamilySymbol.Id);
                        }
                    }
                    foreach (var curFamilySymbolId in familySymbolEleId)
                    {
                        FamilyList.Add(doc.GetElement(curFamilySymbolId));
                    }
                }
            }
            else//选择集中的族
            {
                ICollection<ElementId> familyInstanceEleId = uiDoc.Selection.GetElementIds();
                IList<ElementId> familySymbolEleId = new List<ElementId>();

                foreach (var elementId in familyInstanceEleId)
                {
                    if (doc.GetElement(elementId) is FamilyInstance)
                    {
                        FamilyInstance curFamilyInstance = doc.GetElement(elementId) as FamilyInstance;
                        FamilySymbol curFamilySymbol = curFamilyInstance.Symbol;

                        if (curFamilySymbol.Family.IsInPlace || !curFamilySymbol.Family.IsEditable)
                        {
                            continue;
                        }

                        if (curFamilySymbol.Category == null)
                        {
                            continue;
                        }
                        if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familySymbolEleId.Contains(curFamilySymbol.Id))
                        {
                            familySymbolEleId.Add(curFamilySymbol.Id);
                        }
                    }
                }
                foreach (var curFamilySymbolId in familySymbolEleId)
                {
                    FamilyList.Add(doc.GetElement(curFamilySymbolId));
                }
            }
            SysCacheSubExport.Instance.FamilyTreeSourceList = FamilyList;
            return;
        }

        public string GetName()
        {
            return "LoadFamilyTreeSource";
        }
    }
}
