using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CoreDAL.Configuration;
using CoreDAL.ORM.Extensions;
using CoreDAL.ORM.Handlers;
using CoreDAL.ORM.Interfaces;

namespace CoreDAL.ORM
{
    /// <summary>
    /// Database 파라미터 처리 프로세서
    /// </summary>
    public class DatabaseParameterProcessor
    {
        private readonly IDbParameterHandler _parameterHandler;
        private readonly DatabaseType _dbType;

        public DatabaseParameterProcessor(DatabaseType dbType)
        {
            _dbType = dbType;
            _parameterHandler = CreateParameterHandler(dbType);
        }

        /// <summary>
        /// DB Command에 파라미터 추가
        /// </summary>
        /// <param name="command">DB Command (<see cref="IDbCommand"/>)</param>
        /// <param name="parameters">정의된 파라미터 객체</param>
        public void AddParameters(IDbCommand command, ISQLParam parameters)
        {
            AddParameters(command.Connection, command, parameters);
        }

        /// <summary>
        /// 유효한 파라미터만 추가 (ISQLParam 버전)
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/></param>
        /// <param name="command"><see cref="IDbCommand"/></param>
        /// <param name="parameters"><see cref="SQLParam"/></param>
        public void AddParameters(IDbConnection connection, IDbCommand command, ISQLParam parameters)
        {
            if (parameters == null)
            {
                return;
            }

            var procedureParameters = _parameterHandler.GetProcedureParameters(connection, command);
            if (procedureParameters == null || procedureParameters.Count == 0)
            {
                return;
            }

            var propertyMap = parameters.GetType().GetProperties()
                .Select(prop => new
                {
                    Property = prop,
                    Attribute = prop.GetCustomAttribute<DbParameterAttribute>()
                })
                .Where(x => x.Attribute != null)
                .ToDictionary(
                    x => x.Attribute.Name ?? x.Property.Name,
                    x => x.Property,
                    StringComparer.OrdinalIgnoreCase
                    );

            foreach (IDbDataParameter dataParameter in procedureParameters)
            {
                var normalizedName = _parameterHandler.NormalizeParameterName(dataParameter.ParameterName);
                bool isOutputParam = dataParameter.Direction == ParameterDirection.Output || dataParameter.Direction == ParameterDirection.InputOutput;

                if (propertyMap.TryGetValue(normalizedName, out var propertyInfo))
                {
                    var value = propertyInfo.GetValue(parameters);
                    if (value != null || isOutputParam)
                    {
                        var parameter = _parameterHandler.CreateParameter(dataParameter, value);
                        command.Parameters.Add(parameter);
                    }
                }
                else if (isOutputParam)
                {
                    var parameter = _parameterHandler.CreateParameter(dataParameter, DBNull.Value);
                    command.Parameters.Add(parameter);
                }
            }
        }

        /// <summary>
        /// 유효한 파라미터만 추가 (Dictionary 버전)
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/></param>
        /// <param name="command"><see cref="IDbCommand"/></param>
        /// <param name="parameters"><see cref="Dictionary{TKey,TValue}"/></param>
        public void AddParameters(IDbConnection connection, IDbCommand command, Dictionary<string, object> parameters)
        {
            var procedureParameters = _parameterHandler.GetProcedureParameters(connection, command);
            if (procedureParameters == null || procedureParameters.Count == 0)
            {
                return;
            }

            var normalizedParam = parameters != null
                ? new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (IDbDataParameter dataParameter in procedureParameters)
            {
                var normalizedName = _parameterHandler.NormalizeParameterName(dataParameter.ParameterName);
                bool isOutputParam = dataParameter.Direction == ParameterDirection.Output ||
                                     dataParameter.Direction == ParameterDirection.InputOutput;

                if (normalizedParam.TryGetValue(normalizedName, out var value))
                {
                    var parameter = _parameterHandler.CreateParameter(dataParameter, value);
                    command.Parameters.Add(parameter);
                }
                else if (isOutputParam)
                {
                    var parameter = _parameterHandler.CreateParameter(dataParameter, DBNull.Value);
                    command.Parameters.Add(parameter);
                }
            }
        }

        /// <summary>
        /// Return 파라미터 추가
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/></param>
        /// <param name="command"><see cref="IDbCommand"/></param>
        public void AddReturnParam(IDbConnection connection, IDbCommand command)
        {
            var parameter = _parameterHandler.CreateReturnParameter();
            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// Output 파라미터 결과 값 설정
        /// </summary>
        /// <param name="command">DB Command (<see cref="IDbCommand"/>)</param>
        /// <param name="parameters">결과를 담을 파라미터 객체</param>
        public void SetValueOutputParameters(IDbCommand command, ISQLParam parameters)
        {
            if (parameters == null)
            {
                return;
            }

            _parameterHandler.SetOutputParameter(command, ref parameters);
        }

        /// <summary>
        /// Output 파라미터 결과 값 설정
        /// </summary>
        /// <param name="command">DB Command (<see cref="IDbCommand"/>)</param>
        /// <param name="parameters">결과를 담을 파라미터 객체</param>
        public void SetValueOutputParameters(IDbCommand command, Dictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            _parameterHandler.SetOutputParameter(command, ref parameters);
        }

        public int SetReturnValue(IDbCommand command)
        {
            var returnParam = command.Parameters.Cast<IDbDataParameter>()
                .FirstOrDefault(p => p.Direction == ParameterDirection.ReturnValue);

            return returnParam?.Value as int? ?? -1;
        }

        /// <summary>
        /// ParameterHandler 팩토리
        /// </summary>
        /// <param name="dbType">데이터베이스 종류</param>
        /// <returns><see cref="IDbParameterHandler"/> - 파라미터 핸들러</returns>
        /// <exception cref="ArgumentOutOfRangeException">데이터베이스 종류 없음</exception>
        private IDbParameterHandler CreateParameterHandler(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.MSSQL:
                    return new SqlServerParameterHandler();
                case DatabaseType.ORACLE:
                    return new OracleParameterHandler();
                case DatabaseType.NONE:
                case DatabaseType.END:
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null);
            }
        }
    }
}
