using System.Collections.Generic;

namespace CoreDAL.Configuration.Interface
{
    /// <summary>
    /// 데이터베이스 설정 정보를 가진 컨테이너 인터페이스
    /// </summary>
    public interface IDatabaseSetupContainer
    {
        /// <summary>
        /// 데이터베이스 설정 리스트
        /// </summary>
        IReadOnlyDictionary<string, IDatabaseSetup> Setups { get; }

        /// <summary>
        /// 데이터베이스 설정 가져오기
        /// </summary>
        /// <param name="sectionName">설정에 사용하는 섹션명</param>
        /// <returns>데이터베이스 설정</returns>
        IDatabaseSetup GetSetup(string sectionName);

        /// <summary>
        /// 데이터베이스 설정 업데이트
        /// </summary>
        /// <param name="sectionName">이미 존재하는 설정의 섹션명</param>
        /// <param name="connectionInfo">업데이트할 연결 정보</param>
        void UpdateSetup(string sectionName, IDbConnectionInfo connectionInfo);
    }
}
