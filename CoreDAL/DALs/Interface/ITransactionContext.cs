using System;
using System.Collections.Generic;
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
    }
}

