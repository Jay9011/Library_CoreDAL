using System;
using System.Data;

namespace CoreDAL.ORM
{
    public class SQLResult : IDisposable
    {
        /// <summary>
        /// 쿼리 결과 DataSet
        /// </summary>
        public DataSet DataSet { get; set; }
        /// <summary>
        /// 쿼리 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 쿼리 결과 메시지 (Message)
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 쿼리 결과 값 (RETURN)
        /// </summary>
        public int ReturnValue { get; set; }

        /// <summary>
        /// 해제 여부 플래그
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Constructor (기본 생성자는 실패 결과 반환)
        /// </summary>
        public SQLResult()
        {
            DataSet = null;
            IsSuccess = false;
            Message = string.Empty;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSet">데이터셋</param>
        /// <param name="isSuccess">성공 여부</param>
        /// <param name="message">추가 메시지</param>
        public SQLResult(DataSet dataSet, bool isSuccess, string message = "")
        {
            DataSet = dataSet;
            IsSuccess = isSuccess;
            Message = message;
        }

        /// <summary>
        /// 일반적인 성공 결과 반환
        /// </summary>
        /// <param name="dataSet">쿼리 결과</param>
        /// <returns></returns>
        public static SQLResult Success(DataSet dataSet, string message = "")
        {
            return new SQLResult(dataSet, true, message);
        }

        /// <summary>
        /// 성공 결과 반환 (추가 RETURN 값)
        /// </summary>
        /// <param name="dataSet">쿼리 결과</param>
        /// <param name="returnValue">RETURN 값</param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static SQLResult Success(DataSet dataSet, int returnValue, string message = "")
        {
            var result = new SQLResult(dataSet, true, message);
            result.ReturnValue = returnValue;
            return result;
        }

        /// <summary>
        /// 일반적인 실패 결과 반환
        /// </summary>
        /// <returns></returns>
        public static SQLResult Fail(string message = "")
        {
            return new SQLResult(null, false, message);
        }

        /// <summary>
        /// 실패 결과 반환 (추가 RETURN 값)
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="returnValue"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static SQLResult Fail(int returnValue, string message = "")
        {
            var result = new SQLResult(null, false, message);
            result.ReturnValue = returnValue;
            return result;
        }

        /// <summary>
        /// Dispose 메서드
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose 메서드 (protected virtual)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                DataSet?.Dispose();
                DataSet = null;
            }
            _disposed = true;
        }
    }
}
