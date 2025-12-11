using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CoreDAL.ORM.Extensions;
using CoreDAL.ORM.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace CoreDAL.ORM.Handlers
{
    public class OracleParameterHandler : IDbParameterHandler
    {
        public string NormalizeParameterName(string paramName)
        {
            return paramName.TrimStart(':');
        }

        public string AddParameterPrefix(string paramName)
        {
            return paramName.StartsWith(":") ? paramName : ":" + paramName;
        }

        public IDbDataParameter CreateParameter(IDbDataParameter parameter, object value)
        {
            throw new NotImplementedException();
        }

        public IDbDataParameter CreateReturnParameter()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parameter 특별 옵션 설정
        /// </summary>
        /// <param name="parameter">만들어진 각 DB의 파라미터</param>
        /// <param name="attr">속성 (<see cref="DbParameterAttribute"/>)</param>
        public void ConfigureParameter(IDbDataParameter parameter, DbParameterAttribute attr)
        {
            // var oracleParam = parameter as OracleParameter;
            //
            // if (attr.Direction == ParameterDirection.Output && attr.DbType == DbType.Object)
            // {
            //     oracleParam.OracleDbType = OracleDbType.RefCursor;
            // }
        }

        /// <summary>
        /// Oracle Procedure의 파라미터명을 가져온다.
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/> - DBConnection</param>
        /// <param name="command"><see cref="IDbCommand"/> - DBCommand</param>
        /// <returns><see cref="HashSet{T}"/> - 파라미터명 리스트 해시셋</returns>
        public HashSet<string> GetProcedureParameterNames(IDbConnection connection, IDbCommand command)
        {
            var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (command is OracleCommand oracleCommand)
                {
                    // 현재 파라미터를 임시 저장
                    var originalParameters = oracleCommand.Parameters.Cast<OracleParameter>().ToList();
                    oracleCommand.Parameters.Clear();

                    // 프로시저의 파라미터 정보를 가져옴
                    OracleCommandBuilder.DeriveParameters(oracleCommand);

                    // 파라미터 이름들을 저장
                    foreach (OracleParameter param in oracleCommand.Parameters)
                    {
                        // Oracle 파라미터는 보통 ":" 접두사가 붙음
                        var paramName = param.ParameterName.TrimStart(':');
                        parameters.Add(paramName);
                    }

                    // 원래 상태로 복구
                    oracleCommand.Parameters.Clear();
                    foreach (var param in originalParameters)
                    {
                        oracleCommand.Parameters.Add(param);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get procedure parameter names.", e);
            }

            return parameters;
        }

        public IReadOnlyList<IDbDataParameter> GetProcedureParameters(IDbConnection connection, IDbCommand command)
        {
            throw new NotImplementedException();
        }

        public void SetOutputParameter(IDbCommand command, ref ISQLParam parameters)
        {
            throw new NotImplementedException();
        }

        public void SetOutputParameter(IDbCommand command, ref Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
