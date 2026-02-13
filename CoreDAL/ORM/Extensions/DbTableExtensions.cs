using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace CoreDAL.ORM.Extensions
{
    /// <summary>
    /// DataTable 관련 확장 메서드
    /// TVP(Table-Valued Parameter) 사용을 위한 DataTable 생성 및 데이터 추가 기능 제공
    /// </summary>
    public static class DbTableExtensions
    {
        #region IEnumerable<T> → DataTable 변환

        /// <summary>
        /// 컬렉션을 DataTable로 변환합니다.
        /// TvpColumnAttribute가 있으면 해당 설정을 사용하고, 없으면 프로퍼티 이름을 컬럼명으로 사용합니다.
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <param name="items">변환할 컬렉션</param>
        /// <returns>생성된 DataTable</returns>
        /// <example>
        /// <code>
        /// var users = new List&lt;UserTvpItem&gt; 
        /// {
        ///     new UserTvpItem { Id = 1, Name = "홍길동" },
        ///     new UserTvpItem { Id = 2, Name = "김철수" }
        /// };
        /// 
        /// DataTable table = users.ToDataTable();
        /// </code>
        /// </example>
        public static DataTable ToDataTable<T>(this IEnumerable<T> items) where T : class
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var typeInfo = TvpTypeCache.GetTypeInfo<T>();
            var table = typeInfo.CreateDataTable();

            foreach (var item in items)
            {
                if (item != null)
                {
                    var values = typeInfo.GetValues(item);
                    table.Rows.Add(values);
                }
            }

            return table;
        }

        /// <summary>
        /// 컬렉션을 DataTable로 변환합니다 (테이블 이름 지정)
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <param name="items">변환할 컬렉션</param>
        /// <param name="tableName">DataTable 이름</param>
        /// <returns>생성된 DataTable</returns>
        public static DataTable ToDataTable<T>(this IEnumerable<T> items, string tableName) where T : class
        {
            var table = items.ToDataTable();
            table.TableName = tableName;
            return table;
        }

        #endregion

        #region DataTable에 행 추가

        /// <summary>
        /// 기존 DataTable에 컬렉션의 데이터를 추가합니다.
        /// DataTable의 컬럼명과 객체의 프로퍼티명(또는 TvpColumnAttribute.Name)을 매칭하여 값을 설정합니다.
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <param name="table">대상 DataTable</param>
        /// <param name="items">추가할 컬렉션</param>
        /// <returns>추가된 행 수</returns>
        /// <example>
        /// <code>
        /// // 수동으로 스키마 생성
        /// var table = new DataTable();
        /// table.Columns.Add("UserId", typeof(int));
        /// table.Columns.Add("UserName", typeof(string));
        /// 
        /// // 확장 메서드로 데이터 추가
        /// var users = new List&lt;UserTvpItem&gt; { ... };
        /// table.AddRows(users);
        /// </code>
        /// </example>
        public static int AddRows<T>(this DataTable table, IEnumerable<T> items) where T : class
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var typeInfo = TvpTypeCache.GetTypeInfo<T>();

            // DataTable 컬럼과 타입 프로퍼티 매핑 생성
            var columnMapping = CreateColumnMapping(table, typeInfo);

            int addedCount = 0;
            foreach (var item in items)
            {
                if (item != null)
                {
                    var row = table.NewRow();

                    foreach (var mapping in columnMapping)
                    {
                        var value = mapping.Getter(item);
                        row[mapping.ColumnIndex] = value ?? DBNull.Value;
                    }

                    table.Rows.Add(row);
                    addedCount++;
                }
            }

            return addedCount;
        }

        /// <summary>
        /// 기존 DataTable에 단일 객체를 추가합니다.
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <param name="table">대상 DataTable</param>
        /// <param name="item">추가할 객체</param>
        /// <returns>추가 성공 여부</returns>
        public static bool AddRow<T>(this DataTable table, T item) where T : class
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (item == null)
                return false;

            var typeInfo = TvpTypeCache.GetTypeInfo<T>();
            var columnMapping = CreateColumnMapping(table, typeInfo);

            var row = table.NewRow();

            foreach (var mapping in columnMapping)
            {
                var value = mapping.Getter(item);
                row[mapping.ColumnIndex] = value ?? DBNull.Value;
            }

            table.Rows.Add(row);
            return true;
        }

        #endregion

        #region 스키마 생성

        /// <summary>
        /// 타입에서 DataTable 스키마만 생성합니다 (데이터 없음)
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <returns>빈 DataTable (스키마만 있음)</returns>
        /// <example>
        /// <code>
        /// // 스키마만 생성
        /// DataTable table = DataTableExtensions.CreateSchema&lt;UserTvpItem&gt;();
        /// 
        /// // 나중에 데이터 추가
        /// table.AddRows(users);
        /// </code>
        /// </example>
        public static DataTable CreateSchema<T>() where T : class
        {
            var typeInfo = TvpTypeCache.GetTypeInfo<T>();
            return typeInfo.CreateDataTable();
        }

        /// <summary>
        /// 타입에서 DataTable 스키마만 생성합니다 (테이블 이름 지정)
        /// </summary>
        /// <typeparam name="T">아이템 타입</typeparam>
        /// <param name="tableName">DataTable 이름</param>
        /// <returns>빈 DataTable (스키마만 있음)</returns>
        public static DataTable CreateSchema<T>(string tableName) where T : class
        {
            var table = CreateSchema<T>();
            table.TableName = tableName;
            return table;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// Dictionary 형태로 행 추가 (동적 사용)
        /// </summary>
        /// <param name="table">대상 DataTable</param>
        /// <param name="values">컬럼명-값 Dictionary</param>
        /// <returns>추가 성공 여부</returns>
        public static bool AddRow(this DataTable table, Dictionary<string, object> values)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (values == null || values.Count == 0)
                return false;

            var row = table.NewRow();

            foreach (var kvp in values)
            {
                if (table.Columns.Contains(kvp.Key))
                {
                    row[kvp.Key] = kvp.Value ?? DBNull.Value;
                }
            }

            table.Rows.Add(row);
            return true;
        }

        /// <summary>
        /// Dictionary 컬렉션으로 여러 행 추가 (동적 사용)
        /// </summary>
        /// <param name="table">대상 DataTable</param>
        /// <param name="rows">컬럼명-값 Dictionary 컬렉션</param>
        /// <returns>추가된 행 수</returns>
        public static int AddRows(this DataTable table, IEnumerable<Dictionary<string, object>> rows)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (rows == null)
                return 0;

            int count = 0;
            foreach (var values in rows)
            {
                if (table.AddRow(values))
                    count++;
            }

            return count;
        }

        #endregion

        #region Internal - 런타임 IEnumerable → DataTable 자동 변환

        /// <summary>
        /// 런타임에 IEnumerable 타입의 객체를 DataTable로 변환합니다.
        /// TVP 파라미터 자동 변환 시 내부적으로 사용됩니다.
        /// </summary>
        /// <param name="enumerable">IEnumerable 객체 (List&lt;T&gt;, T[] 등)</param>
        /// <returns>변환된 DataTable</returns>
        /// <exception cref="ArgumentNullException">enumerable이 null인 경우</exception>
        /// <exception cref="InvalidOperationException">요소 타입을 감지할 수 없거나 유효한 컬럼이 없는 경우</exception>
        internal static DataTable ConvertToDataTable(System.Collections.IEnumerable enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var elementType = GetEnumerableElementType(enumerable.GetType());
            if (elementType == null)
                throw new InvalidOperationException(
                    $"IEnumerable의 요소 타입을 감지할 수 없습니다. " +
                    $"IEnumerable<T> 형태를 사용하세요. " +
                    $"현재 타입: {enumerable.GetType().Name}");

            var typeInfo = TvpTypeCache.GetTypeInfo(elementType);
            if (typeInfo.Columns.Count == 0)
                throw new InvalidOperationException(
                    $"TVP 변환 대상 타입 '{elementType.Name}'에 유효한 컬럼 정보가 없습니다. " +
                    $"[TvpColumn] 어트리뷰트를 사용하거나 public 프로퍼티가 있는 클래스를 사용하세요.");

            var table = typeInfo.CreateDataTable();

            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var values = typeInfo.GetValues(item);
                    table.Rows.Add(values);
                }
            }

            return table;
        }

        /// <summary>
        /// IEnumerable&lt;T&gt;에서 요소 타입 T를 추출합니다.
        /// </summary>
        /// <param name="type">IEnumerable을 구현한 타입</param>
        /// <returns>요소 타입 (감지 불가 시 null)</returns>
        private static Type GetEnumerableElementType(Type type)
        {
            // IEnumerable<T> 인터페이스에서 T 추출
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var elementType = iface.GetGenericArguments()[0];
                    // string의 IEnumerable<char> 등 원시 타입은 제외
                    if (elementType != typeof(char))
                        return elementType;
                }
            }

            // 타입 자체가 IEnumerable<T>인 경우
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// DataTable 컬럼과 타입 프로퍼티 매핑 생성
        /// </summary>
        private static List<ColumnPropertyMapping> CreateColumnMapping(DataTable table, TvpTypeInfo typeInfo)
        {
            var mappings = new List<ColumnPropertyMapping>();

            foreach (var columnInfo in typeInfo.Columns)
            {
                // 컬럼명으로 매칭 (대소문자 무시)
                var columnIndex = FindColumnIndex(table, columnInfo.ColumnName);

                if (columnIndex >= 0)
                {
                    mappings.Add(new ColumnPropertyMapping
                    {
                        ColumnIndex = columnIndex,
                        Getter = columnInfo.Getter
                    });
                }
            }

            return mappings;
        }

        /// <summary>
        /// DataTable에서 컬럼 인덱스 찾기 (대소문자 무시)
        /// </summary>
        private static int FindColumnIndex(DataTable table, string columnName)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (table.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 컬럼-프로퍼티 매핑 정보
        /// </summary>
        private class ColumnPropertyMapping
        {
            public int ColumnIndex { get; set; }
            public Func<object, object> Getter { get; set; }
        }

        #endregion
    }
}
