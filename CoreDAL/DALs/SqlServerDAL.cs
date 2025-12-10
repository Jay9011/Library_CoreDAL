using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.Configuration;
using CoreDAL.Configuration.Interface;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;
using Microsoft.Data.SqlClient;

namespace CoreDAL.DALs
{
    public class SqlServerDAL : ICoreDAL
    {
        private int _timeout;

        #region Singleton

        private static readonly Lazy<SqlServerDAL> _instance = new Lazy<SqlServerDAL>(() => new SqlServerDAL());
        internal static SqlServerDAL Instance => _instance.Value;

        private readonly DatabaseParameterProcessor _parameterProcessor;

        private SqlServerDAL(int timeout = 600)
        {
            _timeout = timeout;
            _parameterProcessor = new DatabaseParameterProcessor(DatabaseType.MSSQL);
        }

        #endregion

        #region Transaction

        /// <summary>
        /// 트랜잭션 컨텍스트 생성 (여러 프로시저를 하나의 트랜잭션으로 묶기)
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <returns>트랜잭션 컨텍스트</returns>
        public ITransactionContext BeginTransaction(string connectionString)
        {
            return new SqlServerTransactionContext(connectionString, _parameterProcessor, _timeout);
        }

        /// <summary>
        /// 트랜잭션 컨텍스트 생성 (여러 프로시저를 하나의 트랜잭션으로 묶기)
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <returns>트랜잭션 컨텍스트</returns>
        public ITransactionContext BeginTransaction(IDatabaseSetup dbSetup)
        {
            if (dbSetup == null)
                throw new ArgumentNullException(nameof(dbSetup));

            return BeginTransaction(dbSetup.GetConnectionString());
        }

        #endregion

        public async Task<SQLResult> TestConnectionAsync(IDatabaseSetup dbSetup)
        {
            if (dbSetup == null)
            {
                return SQLResult.Fail("Database setup is null");
            }

            return await TestConnectionAsync(dbSetup.GetConnectionString());
        }

        public async Task<SQLResult> TestConnectionAsync(string connectionString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    return SQLResult.Success(null, "Connection successful");
                }
            }
            catch (Exception e)
            {
                return SQLResult.Fail(e.Message);
            }
        }

        public SQLResult ExecuteProcedure(IDatabaseSetup dbSetup, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true)
        {
            if (dbSetup == null)
                throw new ArgumentNullException(nameof(dbSetup));

            return ExecuteProcedureInternal(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        public SQLResult ExecuteProcedure(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            if (dbSetup == null)
                throw new ArgumentNullException(nameof(dbSetup));

            return ExecuteProcedureInternal(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        public Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true)
        {
            if (dbSetup == null)
                throw new ArgumentNullException(nameof(dbSetup));

            return ExecuteProcedureInternalAsync(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        public Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            if (dbSetup == null)
                throw new ArgumentNullException(nameof(dbSetup));

            return ExecuteProcedureInternalAsync(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        SQLResult ICoreDAL.ExecuteProcedure(string connectionString, string storedProcedureName, ISQLParam parameters, bool isReturn)
        {
            return ExecuteProcedureInternal(connectionString, storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        SQLResult ICoreDAL.ExecuteProcedure(string connectionString, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn)
        {
            return ExecuteProcedureInternal(connectionString, storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        Task<SQLResult> ICoreDAL.ExecuteProcedureAsync(string connectionString, string storedProcedureName, ISQLParam parameters, bool isReturn)
        {
            return ExecuteProcedureInternalAsync(connectionString, storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        Task<SQLResult> ICoreDAL.ExecuteProcedureAsync(string connectionString, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn)
        {
            return ExecuteProcedureInternalAsync(connectionString, storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        private SQLResult ExecuteProcedureInternal(string connectionString, string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(storedProcedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _timeout
                })
                {
                    parameterSetter(connection, command);
                    if (isReturn)
                    {
                        _parameterProcessor.AddReturnParam(connection, command);
                    }

                    DataSet dataSet = new DataSet();
                    using (var reader = command.ExecuteReader())
                    {
                        do
                        {
                            var table = new DataTable();
                            table.Load(reader);
                            dataSet.Tables.Add(table);
                        } while (!reader.IsClosed);

                        if (dataSet.Tables.Count == 0)
                        {
                            dataSet = null;
                        }
                    }

                    outputParameterHandler?.Invoke(command);

                    return SQLResult.Success(dataSet, _parameterProcessor.SetReturnValue(command));
                }
            }
        }

        private async Task<SQLResult> ExecuteProcedureInternalAsync(string connectionString, string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(storedProcedureName, connection)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = _timeout
                    })
                    {
                        parameterSetter(connection, command);
                        if (isReturn)
                        {
                            _parameterProcessor.AddReturnParam(connection, command);
                        }

                        DataSet dataSet = new DataSet();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            do
                            {
                                var table = new DataTable();
                                table.Load(reader);
                                dataSet.Tables.Add(table);
                            } while (!reader.IsClosed);

                            if (dataSet.Tables.Count == 0)
                            {
                                dataSet = null;
                            }
                        }

                        outputParameterHandler?.Invoke(command);

                        return SQLResult.Success(dataSet, _parameterProcessor.SetReturnValue(command));
                    }
                }
            }
            catch (TaskCanceledException e)
            {
                throw new TimeoutException("Timeout executing stored procedure", e);
            }
        }
    }
}
