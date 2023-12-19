using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
{
    public class LoadFamilySource : IExternalEventHandler
    {
        public FamilySource LoadSource { get; set; } = FamilySource.Document;
        public FamilyType LoadType { get; set; } = FamilyType.SystemFamily;
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

                    if (LoadType == FamilyType.LoadFamily)//文档中——载入族
                    {
                        IList<Element> listFamily = collector.OfClass(typeof(Family)).ToElements();

                        foreach (var element in listFamily)
                        {
                            Family curFamily = element as Family;
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
                    else//文档中——系统族
                    {
                        ElementClassFilter filter = new ElementClassFilter(typeof(FamilySymbol), true);
                        IList<Element> listSystemFamilyType = collector.WhereElementIsElementType().WherePasses(filter).ToElements();

                        foreach (var element in listSystemFamilyType)
                        {
                            if (element.Category == null)
                            {
                                continue;
                            }
                            if (element.Category.CategoryType == CategoryType.Model && !FamilyList.Contains(element))
                            {
                                FamilyList.Add(element);
                            }
                        }
                    }
                }
                else//当前活动视图中族
                {
                    ElementId viewId = doc.ActiveView.Id;
                    collector = new FilteredElementCollector(doc, viewId);
                    IList<ElementId> familySymbolEleId = new List<ElementId>();

                    if (LoadType == FamilyType.LoadFamily)//当前活动视图中——载入族
                    {
                        IList<Element> listFamilyInstance = collector.OfClass(typeof(FamilyInstance)).ToElements();

                        foreach (var element in listFamilyInstance)
                        {
                            FamilyInstance curFamilyInstance = element as FamilyInstance;
                            FamilySymbol curFamilySymbol = curFamilyInstance.Symbol;
                            if (curFamilySymbol.Category == null)
                            {
                                continue;
                            }
                            if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familySymbolEleId.Contains(curFamilySymbol.Id))
                            {
                                familySymbolEleId.Add(curFamilySymbol.Id);
                            }
                            // 无法去重——可能原因：由族实例获取的族类型
                            //if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familyList.Contains(curFamilySymbol))
                            //{
                            //    familyList.Add(curFamilySymbol);
                            //}
                        }
                        foreach (var curFamilySymbolId in familySymbolEleId)
                        {
                            FamilyList.Add(doc.GetElement(curFamilySymbolId));
                        }
                    }
                    else//当前活动视图中——系统族
                    {
                        ElementClassFilter filter = new ElementClassFilter(typeof(FamilyInstance), true);
                        IList<Element> listSystemFamilyInstance = collector.WherePasses(filter).ToElements();

                        foreach (var element in listSystemFamilyInstance)
                        {
                            if (element.Category == null)
                            {
                                continue;
                            }
                            if (element.Category.CategoryType == CategoryType.Model)
                            {
                                ElementId curSystemFamilyTypeElementId = element.GetTypeId();
                                Element curSystemFamilyTypeElement = doc.GetElement(curSystemFamilyTypeElementId);
                                if (curSystemFamilyTypeElement != null && curSystemFamilyTypeElement.Category != null && !familySymbolEleId.Contains(curSystemFamilyTypeElementId))
                                {
                                    familySymbolEleId.Add(curSystemFamilyTypeElementId);
                                }
                                // 无法去重——可能原因：由族实例获取的族类型
                                //if (curSystemFamilyTypeElement != null && curSystemFamilyTypeElement.Category != null && !familyList.Contains(curSystemFamilyTypeElement))
                                //{
                                //    familyList.Add(curSystemFamilyTypeElement);
                                //}
                            }
                        }
                        foreach (var curSystemFamilyTypeId in familySymbolEleId)
                        {
                            FamilyList.Add(doc.GetElement(curSystemFamilyTypeId));
                        }
                    }
                }
            }
            else//选择集中族
            {
                ICollection<ElementId> familyInstanceEleId = uiDoc.Selection.GetElementIds();
                IList<ElementId> familySymbolEleId = new List<ElementId>();

                if (LoadType == FamilyType.LoadFamily)//选择集中——载入族
                {
                    foreach (var elementId in familyInstanceEleId)
                    {
                        if (doc.GetElement(elementId) is FamilyInstance)
                        {
                            FamilyInstance curFamilyInstance = doc.GetElement(elementId) as FamilyInstance;
                            FamilySymbol curFamilySymbol = curFamilyInstance.Symbol;

                            if (curFamilySymbol.Category == null)
                            {
                                continue;
                            }
                            if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familySymbolEleId.Contains(curFamilySymbol.Id))
                            {
                                familySymbolEleId.Add(curFamilySymbol.Id);
                            }
                            // 无法去重——可能原因：由族实例获取的族类型
                            //if (curFamilySymbol.Category.CategoryType == CategoryType.Model && !familyList.Contains(curFamilySymbol))
                            //{
                            //    familyList.Add(curFamilySymbol);
                            //}
                        }
                    }
                    foreach (var curFamilySymbolId in familySymbolEleId)
                    {
                        FamilyList.Add(doc.GetElement(curFamilySymbolId));
                    }
                }
                else//选择集中——系统族
                {
                    foreach (ElementId elementId in familyInstanceEleId)
                    {
                        var curFamilyInstanceElement = doc.GetElement(elementId);
                        if (curFamilyInstanceElement.Category == null)
                        {
                            continue;
                        }
                        if (curFamilyInstanceElement.Category.CategoryType == CategoryType.Model)
                        {
                            if (!(curFamilyInstanceElement is FamilyInstance))//排除载入族
                            {
                                ElementId curSystemFamilyTypeElementId = curFamilyInstanceElement.GetTypeId();
                                Element curSystemFamilyTypeElement = doc.GetElement(curSystemFamilyTypeElementId);

                                if (curSystemFamilyTypeElement != null && curSystemFamilyTypeElement.Category != null && !familySymbolEleId.Contains(curSystemFamilyTypeElementId))
                                {
                                    familySymbolEleId.Add(curSystemFamilyTypeElementId);
                                }
                            }
                        }
                    }
                    foreach (var curSystemFamilyTypeId in familySymbolEleId)
                    {
                        FamilyList.Add(doc.GetElement(curSystemFamilyTypeId));
                    }
                }
            }
            SysCacheSubEdit.Instance.FamilySourceList = FamilyList;
            return;
        }

        public string GetName()
        {
            return "LoadFamilySource";
        }
    }
}
