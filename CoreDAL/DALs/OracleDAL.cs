using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.Configuration;
using CoreDAL.Configuration.Interface;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CoreDAL.DALs
{
    public class OracleDAL : ICoreDAL
    {
        private int _timeout;

        #region Singleton

        private static readonly Lazy<OracleDAL> _instance = new Lazy<OracleDAL>(() => new OracleDAL());
        internal static OracleDAL Instance => _instance.Value;

        private readonly DatabaseParameterProcessor _parameterProcessor;

        private OracleDAL(int timeout = 600)
        {
            _timeout = timeout;
            _parameterProcessor = new DatabaseParameterProcessor(DatabaseType.ORACLE);
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
                using (OracleConnection connection = new OracleConnection(connectionString))
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
            {
                throw new ArgumentNullException(nameof(dbSetup));
            }

            return ExecuteProcedureInternal(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        public SQLResult ExecuteProcedure(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            if (dbSetup == null)
            {
                throw new ArgumentNullException(nameof(dbSetup));
            }

            return ExecuteProcedureInternal(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                null,
                isReturn
            );
        }

        public Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true)
        {
            if (dbSetup == null)
            {
                throw new ArgumentNullException(nameof(dbSetup));
            }

            return ExecuteProcedureInternalAsync(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        public Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            if (dbSetup == null)
            {
                throw new ArgumentNullException(nameof(dbSetup));
            }

            return ExecuteProcedureInternalAsync(dbSetup.GetConnectionString(), storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                null,
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
                null,
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
                null,
                isReturn
            );
        }

        private SQLResult ExecuteProcedureInternal(string connectionString, string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            using (var connection = new OracleConnection(connectionString))
            {
                connection.Open();

                using (var command = new OracleCommand(storedProcedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _timeout
                })
                {
                    parameterSetter(connection, command);

                    var dataSet = new DataSet();
                    command.ExecuteNonQuery();

                    ProcessRefCursors(command, dataSet);

                    if (dataSet.Tables.Count == 0)
                    {
                        dataSet = null;
                    }

                    outputParameterHandler?.Invoke(command);

                    return SQLResult.Success(dataSet);
                }
            }
        }

        private async Task<SQLResult> ExecuteProcedureInternalAsync(string connectionString, string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            try
            {
                using (var connection = new OracleConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand(storedProcedureName, connection)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = _timeout
                    })
                    {
                        parameterSetter(connection, command);

                        var dataSet = new DataSet();
                        await command.ExecuteNonQueryAsync();

                        ProcessRefCursors(command, dataSet);

                        if (dataSet.Tables.Count == 0)
                        {
                            dataSet = null;
                        }

                        outputParameterHandler?.Invoke(command);

                        return SQLResult.Success(dataSet);
                    }
                }
            }
            catch (TaskCanceledException e)
            {
                throw new TimeoutException("Timeout executing stored procedure", e);
            }
        }

        private void ProcessRefCursors(OracleCommand command, DataSet dataSet)
        {
            foreach (OracleParameter parameter in command.Parameters)
            {
                if (parameter.OracleDbType == OracleDbType.RefCursor &&
                    (parameter.Direction == ParameterDirection.Output ||
                     parameter.Direction == ParameterDirection.InputOutput))
                {
                    if (parameter.Value != DBNull.Value && parameter.Value != null)
                    {
                        OracleRefCursor refCursor = (OracleRefCursor)parameter.Value;
                        using (var reader = refCursor.GetDataReader())
                        {
                            var table = new DataTable();
                            table.Load(reader);
                            dataSet.Tables.Add(table);
                        }
                    }
                }
            }
        }
    }
}
