using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;

namespace CoreDAL.DALs.Interface
{
    /// <summary>
    /// 트랜잭션 컨텍스트 인터페이스
    /// 여러 프로시저를 하나의 트랜잭션으로 묶어 실행할 수 있습니다.
    /// </summary>
    public interface ITransactionContext : IDisposable
    {
        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (동기)
        /// </summary>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <param name="isReturn">RETURN 값 사용 여부</param>
        /// <returns>실행 결과</returns>
        SQLResult ExecuteProcedure(string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (동기, Dictionary 파라미터)
        /// </summary>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <param name="isReturn">RETURN 값 사용 여부</param>
        /// <returns>실행 결과</returns>
        SQLResult ExecuteProcedure(string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (비동기)
        /// </summary>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <param name="isReturn">RETURN 값 사용 여부</param>
        /// <returns>실행 결과</returns>
        Task<SQLResult> ExecuteProcedureAsync(string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 트랜잭션 내에서 프로시저 실행 (비동기, Dictionary 파라미터)
        /// </summary>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <param name="isReturn">RETURN 값 사용 여부</param>
        /// <returns>실행 결과</returns>
        Task<SQLResult> ExecuteProcedureAsync(string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);

        /// <summary>
        /// 트랜잭션 커밋
        /// </summary>
        void Commit();

        /// <summary>
        /// 트랜잭션 롤백
        /// </summary>
        void Rollback();

        /// <summary>
        /// 트랜잭션 진행 중 격리 수준 변경
        /// </summary>
        /// <param name="isolationLevel">변경할 격리 수준</param>
        /// <remarks>
        /// 주의사항:
        /// - 격리 수준 변경은 세션 레벨에서 적용되며, 이후 실행되는 모든 프로시저에 영향을 줍니다.
        /// - SELECT 전용 프로시저 실행 전 ReadUncommitted로 변경하고, 완료 후 원래 수준으로 복원하는 패턴 권장.
        /// - 격리 수준 변경 후 원래 수준으로 복원하지 않으면 트랜잭션 내 모든 후속 작업에 영향을 줍니다.
        /// 
        /// 사용 예:
        /// <code>
        /// using (var tran = _coreDAL.BeginTransaction(connectionString))
        /// {
        ///     tran.SetIsolationLevel(IsolationLevel.ReadUncommitted);
        ///     await tran.ExecuteProcedureAsync("SP_Select", params);  // Dirty Read 허용
        ///     
        ///     tran.SetIsolationLevel(IsolationLevel.ReadCommitted);
        ///     await tran.ExecuteProcedureAsync("SP_Insert", params);  // 기본 격리 수준
        ///     
        ///     tran.Commit();
        /// }
        /// </code>
        /// </remarks>
        void SetIsolationLevel(IsolationLevel isolationLevel);
    }
}

