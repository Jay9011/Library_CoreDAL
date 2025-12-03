using CoreDAL.DALs.Interface;

namespace CoreDAL.Configuration.Interface
{
    /// <summary>
    /// 데이터베이스 설정 정보를 가진 인터페이스
    /// </summary>
    public interface IDatabaseSetup
    {
        /// <summary>
        /// 데이터베이스 타입
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// DAL 객체
        /// </summary>
        ICoreDAL DAL { get; }

        /// <summary>
        /// 연결 문자열 가져오기
        /// </summary>
        /// <returns>연결 문자열</returns>
        string GetConnectionString();

        /// <summary>
        /// DB 연결 설정 가져오기
        /// </summary>
        /// <returns>DB 연결 설정 정보</returns>
        IDbConnectionInfo GetConnectionInfo();

        /// <summary>
        /// DB 연결 설정 변경
        /// </summary>
        /// <param name="connectionInfo">DB 연결 설정 정보</param>
        void UpdateConnectionInfo(IDbConnectionInfo connectionInfo);
    }
}
