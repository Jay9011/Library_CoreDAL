using System.Collections.Generic;

namespace CoreDAL.Configuration.Interface
{
    /// <summary>
    /// 데이터베이스 연결 정보 모델 인터페이스
    /// </summary>
    public interface IDbConnectionInfo
    {
        /// <summary>
        /// 데이터베이스 타입
        /// </summary>
        DatabaseType DbType { get; }

        /// <summary>
        /// DB 연결 문자열을 가져온다.
        /// </summary>
        /// <returns></returns>
        string ToConnectionString();

        /// <summary>
        /// DB 연결 문자열이 유효한지 확인한다.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        bool Validate(out string errorMessage);

        /// <summary>
        /// 설정 정보를 가져온다.
        /// </summary>
        /// <param name="settings"></param>
        IDbConnectionInfo LoadFromSettings(Dictionary<string, string> settings);

        /// <summary>
        /// 설정 정보로 변환한다.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> ToSettings();
    }
}
