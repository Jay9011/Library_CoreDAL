using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.Configuration.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Interfaces;

namespace CoreDAL.DALs.Interface
{
    /// <summary>
    /// Database 연결에 핵심이 되는 CoreDAL 인터페이스
    /// </summary>
    public interface ICoreDAL
    {
        #region Transaction

        /// <summary>
        /// 트랜잭션 컨텍스트 생성 (여러 프로시저를 하나의 트랜잭션으로 묶기)
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <param name="isolationLevel">트랜잭션 격리 수준 (기본값: ReadCommitted)</param>
        /// <returns>트랜잭션 컨텍스트</returns>
        /// <remarks>
        /// 격리 수준 선택 가이드:
        /// - ReadUncommitted: SELECT 전용 쿼리에 적합 (Lock 없음, Dirty Read 허용)
        /// - ReadCommitted: 기본값, 일반적인 트랜잭션에 적합
        /// - RepeatableRead: 동일 데이터 반복 읽기가 필요한 경우
        /// - Serializable: 가장 강력한 격리, 동시성 낮음
        /// - Snapshot: MVCC 기반, Lock 없이 일관된 읽기 (DB 설정 필요)
        /// </remarks>
        ITransactionContext BeginTransaction(string connectionString, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// 트랜잭션 컨텍스트 생성 (여러 프로시저를 하나의 트랜잭션으로 묶기)
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <param name="isolationLevel">트랜잭션 격리 수준 (기본값: ReadCommitted)</param>
        /// <returns>트랜잭션 컨텍스트</returns>
        /// <remarks>
        /// 격리 수준 선택 가이드:
        /// - ReadUncommitted: SELECT 전용 쿼리에 적합 (Lock 없음, Dirty Read 허용)
        /// - ReadCommitted: 기본값, 일반적인 트랜잭션에 적합
        /// - RepeatableRead: 동일 데이터 반복 읽기가 필요한 경우
        /// - Serializable: 가장 강력한 격리, 동시성 낮음
        /// - Snapshot: MVCC 기반, Lock 없이 일관된 읽기 (DB 설정 필요)
        /// </remarks>
        ITransactionContext BeginTransaction(IDatabaseSetup dbSetup, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        #endregion

        /// <summary>
        /// 연결 테스트
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <returns></returns>
        Task<SQLResult> TestConnectionAsync(IDatabaseSetup dbSetup);

        /// <summary>
        /// 연결 테스트
        /// </summary>
        /// <param name="connectionString">연결 문자열</param>
        /// <returns></returns>
        Task<SQLResult> TestConnectionAsync(string connectionString);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <returns></returns>
        SQLResult ExecuteProcedure(IDatabaseSetup dbSetup, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <returns></returns>
        SQLResult ExecuteProcedure(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <returns></returns>
        Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="dbSetup">DB 설정 파일</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <returns></returns>
        Task<SQLResult> ExecuteProcedureAsync(IDatabaseSetup dbSetup, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <returns></returns>
        SQLResult ExecuteProcedure(string connectionString, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <returns></returns>
        SQLResult ExecuteProcedure(string connectionString, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">파라미터</param>
        /// <returns></returns>
        Task<SQLResult> ExecuteProcedureAsync(string connectionString, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);

        /// <summary>
        /// 프로시저 실행 함수
        /// </summary>
        /// <param name="connectionString">DB 연결 문자열</param>
        /// <param name="storedProcedureName">프로시저명</param>
        /// <param name="parameters">Dictionary 형태의 파라미터</param>
        /// <returns></returns>
        Task<SQLResult> ExecuteProcedureAsync(string connectionString, string storedProcedureName, Dictionary<string, object> parameters, bool isReturn = true);
    }
}
