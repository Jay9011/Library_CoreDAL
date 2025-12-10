using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreDAL.ORM.Extensions
{
    /// <summary>
    /// TVP 타입 정보를 캐싱하여 리플렉션 오버헤드를 최소화합니다.
    /// 컴파일된 델리게이트를 사용하여 프로퍼티 접근 성능을 최적화합니다.
    /// </summary>
    internal static class TvpTypeCache
    {
        /// <summary>
        /// 타입별 컬럼 정보 캐시
        /// </summary>
        private static readonly ConcurrentDictionary<Type, TvpTypeInfo> _typeCache = new ConcurrentDictionary<Type, TvpTypeInfo>();

        /// <summary>
        /// 타입의 TVP 정보를 가져옵니다 (캐시됨)
        /// </summary>
        /// <typeparam name="T">TVP 아이템 타입</typeparam>
        /// <returns>TVP 타입 정보</returns>
        public static TvpTypeInfo GetTypeInfo<T>()
        {
            return GetTypeInfo(typeof(T));
        }

        /// <summary>
        /// 타입의 TVP 정보를 가져옵니다 (캐시됨)
        /// </summary>
        /// <param name="type">TVP 아이템 타입</param>
        /// <returns>TVP 타입 정보</returns>
        public static TvpTypeInfo GetTypeInfo(Type type)
        {
            return _typeCache.GetOrAdd(type, CreateTypeInfo);
        }

        /// <summary>
        /// 타입 정보 생성 (최초 1회만 실행)
        /// [TvpColumn] 어트리뷰트가 있는 프로퍼티만 TVP 컬럼으로 포함됩니다.
        /// </summary>
        private static TvpTypeInfo CreateTypeInfo(Type type)
        {
            var propertiesWithAttribute = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<TvpColumnAttribute>()
                })
                .Where(x => x.Attribute != null)  // [TvpColumn]이 있는 프로퍼티만 포함
                .ToList();

            // [TvpColumn]이 하나도 없으면 모든 프로퍼티를 포함 (하위 호환성)
            List<TvpColumnInfo> columns;
            if (propertiesWithAttribute.Any())
            {
                // [TvpColumn]이 있는 프로퍼티만 사용
                columns = propertiesWithAttribute
                    .Select((x, index) => new TvpColumnInfo
                    {
                        PropertyName = x.Property.Name,
                        ColumnName = x.Attribute.Name ?? x.Property.Name,
                        ColumnType = GetDataColumnType(x.Property.PropertyType),
                        Order = x.Attribute.Order != int.MaxValue ? x.Attribute.Order : index,
                        MaxLength = x.Attribute.MaxLength,
                        IsNullable = x.Attribute.IsNullable,
                        Getter = CreateGetter(type, x.Property),
                        PropertyType = x.Property.PropertyType
                    })
                    .OrderBy(c => c.Order)
                    .ToList();
            }
            else
            {
                // [TvpColumn]이 없으면 모든 public 프로퍼티 사용 (하위 호환성)
                columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead)
                    .Select((p, index) => new TvpColumnInfo
                    {
                        PropertyName = p.Name,
                        ColumnName = p.Name,
                        ColumnType = GetDataColumnType(p.PropertyType),
                        Order = index,
                        MaxLength = -1,
                        IsNullable = IsNullableType(p.PropertyType),
                        Getter = CreateGetter(type, p),
                        PropertyType = p.PropertyType
                    })
                    .ToList();
            }

            return new TvpTypeInfo
            {
                Type = type,
                Columns = columns
            };
        }

        /// <summary>
        /// 프로퍼티 Getter 델리게이트 생성 (컴파일된 Expression)
        /// 리플렉션보다 약 10배 빠름
        /// </summary>
        private static Func<object, object> CreateGetter(Type type, PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instance, type);
            var propertyAccess = Expression.Property(castInstance, property);
            var castResult = Expression.Convert(propertyAccess, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(castResult, instance);
            return lambda.Compile();
        }

        /// <summary>
        /// Nullable 타입에서 기본 타입 추출
        /// </summary>
        private static Type GetDataColumnType(Type propertyType)
        {
            // Nullable<T>인 경우 T 반환
            var underlyingType = Nullable.GetUnderlyingType(propertyType);
            return underlyingType ?? propertyType;
        }

        /// <summary>
        /// Nullable 타입 여부 확인
        /// </summary>
        private static bool IsNullableType(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// 캐시 초기화 (테스트용)
        /// </summary>
        public static void ClearCache()
        {
            _typeCache.Clear();
        }
    }

    /// <summary>
    /// TVP 타입 정보
    /// </summary>
    internal class TvpTypeInfo
    {
        /// <summary>
        /// 원본 타입
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// 컬럼 정보 목록 (순서대로 정렬됨)
        /// </summary>
        public List<TvpColumnInfo> Columns { get; set; }

        /// <summary>
        /// DataTable 스키마 생성
        /// </summary>
        public DataTable CreateDataTable()
        {
            var table = new DataTable();

            foreach (var column in Columns)
            {
                var dataColumn = new DataColumn(column.ColumnName, column.ColumnType)
                {
                    AllowDBNull = column.IsNullable
                };

                if (column.MaxLength > 0)
                {
                    dataColumn.MaxLength = column.MaxLength;
                }

                table.Columns.Add(dataColumn);
            }

            return table;
        }

        /// <summary>
        /// 객체에서 값 배열 추출 (컬럼 순서대로)
        /// </summary>
        public object[] GetValues(object item)
        {
            var values = new object[Columns.Count];

            for (int i = 0; i < Columns.Count; i++)
            {
                var value = Columns[i].Getter(item);
                values[i] = value ?? DBNull.Value;
            }

            return values;
        }
    }

    /// <summary>
    /// TVP 컬럼 정보
    /// </summary>
    internal class TvpColumnInfo
    {
        /// <summary>
        /// 프로퍼티 이름
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// DataTable 컬럼명
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 컬럼 타입 (Nullable 해제됨)
        /// </summary>
        public Type ColumnType { get; set; }

        /// <summary>
        /// 원본 프로퍼티 타입
        /// </summary>
        public Type PropertyType { get; set; }

        /// <summary>
        /// 컬럼 순서
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 최대 길이
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// NULL 허용 여부
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 컴파일된 Getter 델리게이트
        /// </summary>
        public Func<object, object> Getter { get; set; }
    }
}
