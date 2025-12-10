using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using CoreDAL.ORM.Extensions;
using CoreDAL.ORM.Interfaces;
using Microsoft.Data.SqlClient;

namespace CoreDAL.ORM.Handlers
{
    public class SqlServerParameterHandler : IDbParameterHandler
    {
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

                if (!string.IsNullOrEmpty(sqlParameter.TypeName))
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
        /// Procedure의 파라미터 컬렉션을 가져온다.
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/> - DBConnection</param>
        /// <param name="command"><see cref="IDbCommand"/> - DBCommand</param>
        /// <returns> <see cref="DbParameterCollection"/> - 파라미터 컬렉션</returns>
        public DbParameterCollection GetProcedureParameterCollection(IDbConnection connection, IDbCommand command)
        {
            try
            {
                if (command is SqlCommand sqlCommand)
                {
                    var tmpCommand = new SqlCommand(sqlCommand.CommandText, connection as SqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure,
                        Transaction = sqlCommand.Transaction
                    };

                    // 프로시저의 파라미터 정보를 가져옴
                    SqlCommandBuilder.DeriveParameters(tmpCommand);

                    return tmpCommand.Parameters;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get procedure parameter names.", e);
            }

            return null;
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
