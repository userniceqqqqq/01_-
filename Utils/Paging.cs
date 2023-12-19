using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule
{
    /// <summary>
    /// 分页类——页码从0开始
    /// </summary>
    class Paging
    {
        //当前页码
        public int PageIndex { get; set; }

        //当前页码的对象
        List<FamilyObject> PagedList = new List<FamilyObject>();

        /// <summary>
        /// 获取对应页的对象<FamilyObject>
        /// </summary>
        /// <param name="需要分页的对象"></param>
        /// <param name="每页个数"></param>
        /// <returns> List<FamilyObject></returns>
        public List<FamilyObject> SetPaging(List<FamilyObject> ListToPage, int RecordsPerPage)
        {
            int PageGroup = PageIndex * RecordsPerPage;
            PagedList = ListToPage.Skip(PageGroup).Take(RecordsPerPage).ToList(); //跳过指定位置，然后取后面多少个对象
            return PagedList;
        }

        /// <summary>
        /// 上一页
        /// </summary>
        /// <param name="需要分页的对象"></param>
        /// <param name="每页个数"></param>
        /// <returns> List<FamilyObject></returns>
        public List<FamilyObject> Previous(List<FamilyObject> ListToPage, int RecordsPerPage)
        {
            PageIndex--;
            if (PageIndex <= 0)
            {
                PageIndex = 0;
            }
            PagedList = SetPaging(ListToPage, RecordsPerPage);
            return PagedList;//返回上一页的对象
        }

        /// <summary>
        /// 下一页
        /// </summary>
        /// <param name="需要分页的对象"></param>
        /// <param name="每页个数"></param>
        /// <returns> List<FamilyObject></returns>
        public List<FamilyObject> Next(List<FamilyObject> ListToPage, int RecordsPerPage)
        {
            PageIndex++;
            if (ListToPage.Count % RecordsPerPage == 0 && PageIndex >= ListToPage.Count / RecordsPerPage)//判断被整除且页码超出最大页数的情况
            {
                PageIndex = (ListToPage.Count / RecordsPerPage) - 1;
            }
            else if (PageIndex >= ListToPage.Count / RecordsPerPage)//判断不被整除且页码超出最大页数的情况
            {
                PageIndex = ListToPage.Count / RecordsPerPage;
            }
            PagedList = SetPaging(ListToPage, RecordsPerPage);
            return PagedList;//返回下一页的对象
        }

        /// <summary>
        /// 首页
        /// </summary>
        /// <param name="需要分页的对象"></param>
        /// <param name="每页个数"></param>
        /// <returns> List<FamilyObject></returns>
        public List<FamilyObject> First(List<FamilyObject> ListToPage, int RecordsPerPage)
        {
            PageIndex = 0;
            PagedList = SetPaging(ListToPage, RecordsPerPage);
            return PagedList;////返回首页的对象
        }

        /// <summary>
        /// 尾页
        /// </summary>
        /// <param name="需要分页的对象"></param>
        /// <param name="每页个数"></param>
        /// <returns> List<FamilyObject></returns>
        public List<FamilyObject> Last(List<FamilyObject> ListToPage, int RecordsPerPage)
        {
            if (ListToPage.Count % RecordsPerPage == 0)//判断被整除
            {
                PageIndex = (ListToPage.Count / RecordsPerPage) - 1;
            }
            else//判断不被整除
            {
                PageIndex = ListToPage.Count / RecordsPerPage;
            }
            PagedList = SetPaging(ListToPage, RecordsPerPage);
            return PagedList;
        }
    }
}

