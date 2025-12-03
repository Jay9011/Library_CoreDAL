namespace CoreDAL.ORM.Interfaces
{
    /// <summary>
    /// SQL 파라미터 인터페이스
    /// </summary>
    public interface ISQLParam
    {
        /// <summary>
        /// 출력 파라미터에 값 할당
        /// </summary>
        /// <param name="propertyName">프로퍼티명</param>
        /// <param name="value">값</param>
        void SetOutputParameterValue(string propertyName, object value);
    }
}
