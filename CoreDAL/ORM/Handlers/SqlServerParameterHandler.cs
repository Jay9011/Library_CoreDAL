using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CoreDAL.ORM.Extensions;
using CoreDAL.ORM.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace CoreDAL.ORM.Handlers
{
    public class SqlServerParameterHandler : IDbParameterHandler
    {
        #region Parameter Cache

        /// <summary>
        /// 프로시저 파라미터 캐시
        /// </summary>
        private static readonly MemoryCache _parameterCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000
        });

        /// <summary>
        /// 캐시 엔트리 옵션
        /// - 슬라이딩 만료: 마지막 접근 후 30분
        /// - 절대 만료: 최대 2시간
        /// - 크기: 1 (프로시저 1개 = 1 단위)
        /// </summary>
        private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetPriority(CacheItemPriority.Normal);

        /// <summary>
        /// 캐시 키 생성 (서버+DB+프로시저명 조합)
        /// </summary>
        /// <param name="connection">DB 연결</param>
        /// <param name="procedureName">프로시저명</param>
        /// <returns>캐시 키</returns>
        private static string GetCacheKey(IDbConnection connection, string procedureName)
        {
            // 연결 문자열에서 서버와 DB를 추출하여 고유 키 생성
            // 같은 프로시저라도 다른 서버/DB면 다른 캐시 엔트리
            return $"{connection.ConnectionString.GetHashCode()}:{procedureName.ToUpperInvariant()}";
        }

        /// <summary>
        /// SqlParameter 배열을 복제 (캐시된 객체 재사용 방지)
        /// </summary>
        /// <param name="source">원본 파라미터 배열</param>
        /// <returns>복제된 파라미터 배열 (IDbDataParameter로 반환)</returns>
        private static IDbDataParameter[] CloneParameters(SqlParameter[] source)
        {
            var cloned = new IDbDataParameter[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var src = source[i];
                cloned[i] = new SqlParameter
                {
                    ParameterName = src.ParameterName,
                    SqlDbType = src.SqlDbType,
                    DbType = src.DbType,
                    Direction = src.Direction,
                    Size = src.Size,
                    Precision = src.Precision,
                    Scale = src.Scale,
                    IsNullable = src.IsNullable,
                    TypeName = NormalizeTvpTypeName(src.TypeName),
                    Value = DBNull.Value  // 값은 초기화
                };
            }
            return cloned;
        }

        /// <summary>
        /// TVP TypeName에서 데이터베이스 이름을 제거합니다.
        /// SQL Server TVP는 "schema.typeName" 형식만 허용하며, 
        /// "database.schema.typeName" 형식을 사용하면 에러가 발생합니다.
        /// </summary>
        /// <param name="typeName">원본 TypeName (database.schema.typeName 또는 schema.typeName)</param>
        /// <returns>정규화된 TypeName (schema.typeName)</returns>
        private static string NormalizeTvpTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // TypeName이 "database.schema.typeName" 형식인 경우 데이터베이스 이름 제거
            // SQL Server에서 SqlCommandBuilder.DeriveParameters()는 3파트 이름을 반환할 수 있음
            var parts = typeName.Split('.');
            if (parts.Length == 3)
            {
                // database.schema.typeName -> schema.typeName
                return $"{parts[1]}.{parts[2]}";
            }

            // 이미 schema.typeName 형식이거나 typeName만 있는 경우 그대로 반환
            return typeName;
        }

        /// <summary>
        /// 캐시 통계 조회 (디버깅/모니터링용)
        /// </summary>
        public static MemoryCacheStatistics GetCacheStatistics()
        {
            return _parameterCache.GetCurrentStatistics();
        }

        /// <summary>
        /// 캐시 수동 제거 (특정 프로시저)
        /// </summary>
        /// <param name="connection">DB 연결</param>
        /// <param name="procedureName">프로시저명</param>
        public static void InvalidateCache(IDbConnection connection, string procedureName)
        {
            var cacheKey = GetCacheKey(connection, procedureName);
            _parameterCache.Remove(cacheKey);
        }

        /// <summary>
        /// 전체 캐시 초기화
        /// </summary>
        public static void ClearCache()
        {
            _parameterCache.Compact(1.0);  // 100% 압축 = 전체 제거
        }

        #endregion

        public string NormalizeParameterName(string paramName)
        {
            return paramName.TrimStart('@');
        }

        public string AddParameterPrefix(string paramName)
        {
            return paramName.StartsWith("@") ? paramName : "@" + paramName;
        }

        /// <summary>
        /// Parameter 생성
        /// </summary>
        /// <param name="parameter"><see cref="IDbDataParameter"/> - 정확한 파라미터</param>
        /// <param name="value">값 (null인 경우 DBNull.Value로 변환)</param>
        /// <returns><see cref="IDbDataParameter"/></returns>
        public IDbDataParameter CreateParameter(IDbDataParameter parameter, object value)
        {
            if (parameter is SqlParameter sqlParameter)
            {
                // null 값은 DBNull.Value로 변환 (OUTPUT 파라미터 등에서 필요)
                var paramValue = value ?? DBNull.Value;

                var param = new SqlParameter(sqlParameter.ParameterName, paramValue)
                {
                    DbType = sqlParameter.DbType,
                    SqlDbType = sqlParameter.SqlDbType,
                    Direction = sqlParameter.Direction,
                    Size = sqlParameter.Size,
                    Precision = sqlParameter.Precision,
                    Scale = sqlParameter.Scale
                };

                // TVP(Table-Valued Parameter) 처리
                if (sqlParameter.SqlDbType == SqlDbType.Structured || !string.IsNullOrEmpty(sqlParameter.TypeName))
                {
                    param.SqlDbType = SqlDbType.Structured;
                    // SQL Server TVP는 "schema.typeName" 형식만 허용하므로 데이터베이스 이름 제거
                    param.TypeName = NormalizeTvpTypeName(sqlParameter.TypeName);

                    // DataTable이 아닌 경우 에러 메시지 제공
                    if (value != null && !(value is DataTable) && value != DBNull.Value)
                    {
                        throw new ArgumentException(
                            $"TVP 파라미터 '{sqlParameter.ParameterName}'에는 DataTable 타입의 값이 필요합니다. " +
                            $"List<T>.ToDataTable() 확장 메서드를 사용하여 변환하세요. " +
                            $"현재 타입: {value.GetType().Name}",
                            nameof(value));
                    }
                }
                else if (!string.IsNullOrEmpty(sqlParameter.TypeName))
                {
                    param.TypeName = sqlParameter.TypeName;
                }

                return param;
            }

            return null;
        }

        /// <summary>
        /// Return Parameter 생성
        /// </summary>
        /// <returns><see cref="IDbDataParameter"/></returns>
        public IDbDataParameter CreateReturnParameter()
        {
            var param = new SqlParameter("RETURN_VALUE", SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };

            return param;
        }

        /// <summary>
        /// Procedure의 파라미터 정보를 가져온다.
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/> - DBConnection</param>
        /// <param name="command"><see cref="IDbCommand"/> - DBCommand</param>
        /// <returns>파라미터 정보 목록 (읽기 전용)</returns>
        public IReadOnlyList<IDbDataParameter> GetProcedureParameters(IDbConnection connection, IDbCommand command)
        {
            try
            {
                if (command is SqlCommand sqlCommand)
                {
                    var cacheKey = GetCacheKey(connection, sqlCommand.CommandText);

                    // 캐시에서 조회 시도
                    if (_parameterCache.TryGetValue(cacheKey, out SqlParameter[] cachedParameters))
                    {
                        // 캐시된 파라미터 복제본 반환 (IDbDataParameter로 캐스팅)
                        return CloneParameters(cachedParameters);
                    }

                    // 캐시 미스: DB에서 파라미터 정보 조회
                    using (var tmpCommandForDerive = new SqlCommand(sqlCommand.CommandText, connection as SqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure,
                        Transaction = sqlCommand.Transaction
                    })
                    {
                        // 프로시저의 파라미터 정보를 가져옴
                        SqlCommandBuilder.DeriveParameters(tmpCommandForDerive);

                        // 파라미터 배열 생성 (TVP TypeName 정규화 포함)
                        var parametersToCache = new SqlParameter[tmpCommandForDerive.Parameters.Count];
                        for (int i = 0; i < tmpCommandForDerive.Parameters.Count; i++)
                        {
                            var src = tmpCommandForDerive.Parameters[i];
                            parametersToCache[i] = new SqlParameter
                            {
                                ParameterName = src.ParameterName,
                                SqlDbType = src.SqlDbType,
                                DbType = src.DbType,
                                Direction = src.Direction,
                                Size = src.Size,
                                Precision = src.Precision,
                                Scale = src.Scale,
                                IsNullable = src.IsNullable,
                                TypeName = NormalizeTvpTypeName(src.TypeName),  // TVP TypeName 정규화
                                Value = DBNull.Value
                            };
                        }

                        // 캐시에 저장
                        _parameterCache.Set(cacheKey, parametersToCache, _cacheEntryOptions);

                        // 복제본 반환 (캐시된 객체 직접 반환 방지)
                        return CloneParameters(parametersToCache);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get procedure parameter names.", e);
            }

            return Array.Empty<IDbDataParameter>();
        }

        /// <summary>
        /// Output Parameter 설정
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        public void SetOutputParameter(IDbCommand command, ref ISQLParam parameters)
        {
            if (parameters == null)
            {
                return;
            }

            var outputParameters = command.Parameters.Cast<SqlParameter>()
                .Where(p => p.Direction == ParameterDirection.Output ||
                            p.Direction == ParameterDirection.InputOutput);

            var properties = parameters.GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<DbParameterAttribute>() != null);

            foreach (var outParam in outputParameters)
            {
                var normalizedParamName = NormalizeParameterName(outParam.ParameterName);
                var property = properties.FirstOrDefault(p =>
                {
                    var attr = p.GetCustomAttribute<DbParameterAttribute>();
                    var propParamName = attr.Name ?? p.Name;
                    return propParamName.Equals(normalizedParamName, StringComparison.OrdinalIgnoreCase);
                });

                if (property != null)
                {
                    parameters.SetOutputParameterValue(property.Name, outParam.Value);
                }
            }
        }

        /// <summary>
        /// Output Parameter 설정
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        public void SetOutputParameter(IDbCommand command, ref Dictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            var outputParameters = command.Parameters.Cast<SqlParameter>()
                .Where(p => p.Direction == ParameterDirection.Output ||
                            p.Direction == ParameterDirection.InputOutput);

            foreach (var param in outputParameters)
            {
                var paramName = NormalizeParameterName(param.ParameterName);
                if (param.Value != DBNull.Value)
                {
                    parameters[paramName] = param.Value;
                }
            }
        }
    }
}
