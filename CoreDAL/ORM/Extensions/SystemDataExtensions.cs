using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;

namespace CoreDAL.ORM.Extensions
{
    public static class SystemDataExtensions
    {
        /// <summary>
        /// DataTable을 객체로 변환
        /// </summary>
        /// <param name="table"></param>
        /// <typeparam name="T">IEnumerable 구현 객체</typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ToObject<T>(this DataTable table)
        {
            if (table == null)
            {
                return Enumerable.Empty<T>();
            }

            var result = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                result.Add(row.ToObject<T>());
            }

            return result;
        }

        /// <summary>
        /// DataRow를 객체로 변환
        /// </summary>
        /// <param name="row"></param>
        /// <typeparam name="T">변환 대상 객체</typeparam>
        /// <returns></returns>
        public static T ToObject<T>(this DataRow row)
        {
            if (row == null)
            {
                return default;
            }

            var rowDict = row.Table.Columns.Cast<DataColumn>()
                .ToDictionary(col => col.ColumnName, col => row[col.ColumnName]);

            string json = JsonConvert.SerializeObject(rowDict);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
