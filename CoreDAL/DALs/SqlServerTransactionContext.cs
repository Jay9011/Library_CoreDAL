using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;
using Microsoft.Data.SqlClient;

namespace CoreDAL.DALs
{
    /// <summary>
    /// SQL Server용 트랜잭션 컨텍스트 구현체
    /// 여러 프로시저를 하나의 트랜잭션으로 묶어 실행할 수 있습니다.
    /// </summary>
    public class SqlServerTransactionContext : ITransactionContext
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly DatabaseParameterProcessor _parameterProcessor;
        private readonly int _timeout;
        private readonly IsolationLevel _isolationLevel;

        private bool _isCommitted = false;
        private bool _isRolledBack = false;
        private bool _disposed = false;

        /// <summary>
        /// SqlServerTransactionContext 생성자
        /// </summary>
        /// <param name="connectionString">연결 문자열</param>
        /// <param name="parameterProcessor">파라미터 프로세서</param>
        /// <param name="timeout">명령 타임아웃 (초)</param>
        /// <param name="isolationLevel">트랜잭션 격리 수준 (기본값: ReadCommitted)</param>
        /// <remarks>
        /// 격리 수준 선택 가이드:
        /// - ReadUncommitted: SELECT 전용 쿼리에 적합 (Lock 없음, Dirty Read 허용)
        /// - ReadCommitted: 기본값, 일반적인 트랜잭션에 적합 (Shared Lock 사용)
        /// - RepeatableRead: 동일 데이터 반복 읽기가 필요한 경우
        /// - Serializable: 가장 강력한 격리, 동시성 낮음
        /// - Snapshot: MVCC 기반, Lock 없이 일관된 읽기 (DB에서 ALLOW_SNAPSHOT_ISOLATION 설정 필요)
        /// </remarks>
        public SqlServerTransactionContext(string connectionString, DatabaseParameterProcessor parameterProcessor, int timeout = 600, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _parameterProcessor = parameterProcessor ?? throw new ArgumentNullException(nameof(parameterProcessor));
            _timeout = timeout;
            _isolationLevel = isolationLevel;

            _connection = new SqlConnection(connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction(_isolationLevel);
        }

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (동기)
        /// </summary>
        public SQLResult ExecuteProcedure(string storedProcedureName, ISQLParam parameters = null, bool isReturn = true)
        {
            ThrowIfDisposedOrCompleted();

            return ExecuteProcedureInternal(storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (동기, Dictionary 파라미터)
        /// </summary>
        public SQLResult ExecuteProcedure(string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            ThrowIfDisposedOrCompleted();

            return ExecuteProcedureInternal(storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (비동기)
        /// </summary>
        public Task<SQLResult> ExecuteProcedureAsync(string storedProcedureName, ISQLParam parameters = null, bool isReturn = true)
        {
            ThrowIfDisposedOrCompleted();

            return ExecuteProcedureInternalAsync(storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (비동기, Dictionary 파라미터)
        /// </summary>
        public Task<SQLResult> ExecuteProcedureAsync(string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true)
        {
            ThrowIfDisposedOrCompleted();

            return ExecuteProcedureInternalAsync(storedProcedureName,
                (connection, command) => _parameterProcessor.AddParameters(connection, command, parameters),
                (command) => _parameterProcessor.SetValueOutputParameters(command, parameters),
                isReturn
            );
        }

        /// <summary>
        /// 트랜잭션 커밋
        /// </summary>
        public void Commit()
        {
            ThrowIfDisposedOrCompleted();

            _transaction.Commit();
            _isCommitted = true;
        }

        /// <summary>
        /// 트랜잭션 롤백
        /// </summary>
        public void Rollback()
        {
            ThrowIfDisposedOrCompleted();

            _transaction.Rollback();
            _isRolledBack = true;
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 리소스 해제 (protected virtual)
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 커밋이나 롤백이 호출되지 않았으면 자동 롤백
                if (!_isCommitted && !_isRolledBack)
                {
                    try
                    {
                        _transaction?.Rollback();
                    }
                    catch
                    {
                        // 롤백 실패는 무시 (이미 롤백되었거나 연결이 끊어진 경우)
                    }
                }

                _transaction?.Dispose();
                _connection?.Dispose();
            }
            _disposed = true;
        }

        /// <summary>
        /// Dispose되었거나 트랜잭션이 완료된 경우 예외 발생
        /// </summary>
        private void ThrowIfDisposedOrCompleted()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SqlServerTransactionContext));

            if (_isCommitted)
                throw new InvalidOperationException("Transaction has already been committed.");

            if (_isRolledBack)
                throw new InvalidOperationException("Transaction has already been rolled back.");
        }

        /// <summary>
        /// 프로시저 실행 내부 구현 (동기)
        /// </summary>
        private SQLResult ExecuteProcedureInternal(string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            using (var command = new SqlCommand(storedProcedureName, _connection, _transaction)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _timeout
            })
            {
                parameterSetter(_connection, command);
                if (isReturn)
                {
                    _parameterProcessor.AddReturnParam(_connection, command);
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

        /// <summary>
        /// 프로시저 실행 내부 구현 (비동기)
        /// </summary>
        private async Task<SQLResult> ExecuteProcedureInternalAsync(string storedProcedureName, Action<IDbConnection, IDbCommand> parameterSetter, Action<IDbCommand> outputParameterHandler, bool isReturn = true)
        {
            try
            {
                using (var command = new SqlCommand(storedProcedureName, _connection, _transaction)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _timeout
                })
                {
                    parameterSetter(_connection, command);
                    if (isReturn)
                    {
                        _parameterProcessor.AddReturnParam(_connection, command);
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
            catch (TaskCanceledException e)
            {
                throw new TimeoutException("Timeout executing stored procedure", e);
            }
        }
    }
}

