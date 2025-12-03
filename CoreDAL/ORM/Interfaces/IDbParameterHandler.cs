using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace CoreDAL.ORM.Interfaces
{
    /// <summary>
    /// DB 파라미터 처리 핸들러 인터페이스
    /// </summary>
    public interface IDbParameterHandler
    {
        /// <summary>
        /// 파라미터명을 일반화
        /// </summary>
        /// <param paramName="paramName">파라미터명</param>
        /// <returns>파라미터 접두사가 제거된 일반 문자</returns>
        string NormalizeParameterName(string paramName);
        /// <summary>
        /// 파라미터명에 접두사 추가
        /// </summary>
        /// <param name="paramName">접두사가 없는 일반 문자</param>
        /// <returns>파라미터 접두사가 붙은 문자</returns>
        string AddParameterPrefix(string paramName);
        /// <summary>
        /// Parameter 생성
        /// </summary>
        /// <param name="parameter">정확한 파라미터</param>
        /// <param name="value">사용자 입력 값</param>
        /// <returns><see cref="IDbDataParameter"/></returns>
        IDbDataParameter CreateParameter(IDbDataParameter parameter, object value);
        /// <summary>
        /// Return Parameter 생성
        /// </summary>
        /// <returns><see cref="IDbDataParameter"/></returns>
        IDbDataParameter CreateReturnParameter();
        /// <summary>
        /// Procedure의 파라미터 컬렉션을 가져온다.
        /// </summary>
        /// <param name="connection"><see cref="IDbConnection"/> - DBConnection</param>
        /// <param name="command"><see cref="IDbCommand"/> - DBCommand</param>
        /// <returns> <see cref="DbParameterCollection"/> - 파라미터 컬렉션</returns>
        DbParameterCollection GetProcedureParameterCollection(IDbConnection connection, IDbCommand command);
        /// <summary>
        /// Output Parameter 설정
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        void SetOutputParameter(IDbCommand command, ref ISQLParam parameters);
        /// <summary>
        /// Output Parameter 설정
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        void SetOutputParameter(IDbCommand command, ref Dictionary<string, object> parameters);
    }
}
