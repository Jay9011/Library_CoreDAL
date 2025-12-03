using System.Data;
using CoreDAL.Configuration;
using CoreDAL.Configuration.Interface;
using CoreDAL.Configuration.Models;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;
using Xunit.Abstractions;

namespace SECUiDEA.CoreDAL.TestProject
{
    /// <summary>
    /// 프로시저 실행 테스트
    /// </summary>
    public class ProcedureTest
    {
        #region 테스트 설정

        private readonly ITestOutputHelper _outputHelper;

        public ProcedureTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// 테스트용 DB 연결 정보 (실제 환경에 맞게 수정 필요)
        /// </summary>
        private static IDbConnectionInfo CreateTestConnectionInfo()
        {
            return new MsSqlConnectionInfo
            {
                Server = "localhost",
                Database = "TEST",
                UserId = "sa",
                Password = "s1access!",
                Port = 1433,
                IntegratedSecurity = false
            };
        }

        /// <summary>
        /// 테스트용 DAL 가져오기
        /// </summary>
        private static ICoreDAL GetTestDAL()
        {
            return DbDALFactory.CreateCoreDal(DatabaseType.MSSQL);
        }

        #endregion

        #region Return Value 테스트

        /// <summary>
        /// 프로시저 Return 값 테스트
        /// -- Return 값으로 입력값의 2배 반환
        /// </summary>
        [Fact]
        public async Task ReturnTest()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new ReturnValueTestParam
            {
                InputValue = 10
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_RETURN_VALUE",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");
            Assert.Equal(20, result.ReturnValue); // 10 * 2 = 20
        }

        /// <summary>
        /// Return Value 테스트용 파라미터 클래스
        /// </summary>
        public class ReturnValueTestParam : SQLParam
        {
            [DbParameter("InputValue", DbType.Int32, ParameterDirection.Input)]
            public int InputValue { get; set; }
        }

        #endregion

        #region Output Parameter 테스트

        /// <summary>
        /// 프로시저 Output 파라미터 테스트
        /// </summary>
        [Fact]
        public async Task OutputParametersTest()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new OutputParamTestParam
            {
                InputValue = 5
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_OUTPUT_PARAMS",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");
            _outputHelper.WriteLine($"Output Parameter 리스트:");
            _outputHelper.WriteLine($"OutputValue: {parameters.OutputValue}");
            _outputHelper.WriteLine($"OutputMessage: {parameters.OutputMessage}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");
            Assert.Equal(0, result.ReturnValue);
            Assert.Equal(15, parameters.OutputValue); // 5 * 3 = 15
            Assert.Equal("처리 완료: 5", parameters.OutputMessage);
        }

        /// <summary>
        /// Output Parameter 테스트용 파라미터 클래스
        /// </summary>
        public class OutputParamTestParam : SQLParam
        {
            [DbParameter("InputValue", DbType.Int32, ParameterDirection.Input)]
            public int InputValue { get; set; }

            [DbParameter("OutputValue", DbType.Int32, ParameterDirection.Output)]
            public int OutputValue { get; set; }

            [DbParameter("OutputMessage", DbType.String, ParameterDirection.Output)]
            public string OutputMessage { get; set; }
        }

        #endregion

        #region Dictionary 파라미터 테스트

        /// <summary>
        /// Dictionary 형태의 파라미터로 프로시저 실행 테스트
        /// </summary>
        [Fact]
        public async Task OutputParametersTest_Dictionary()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new Dictionary<string, object>
            {
                { "Name", "홍길동" },
                { "Age", 25 }
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_DICTIONARY_PARAMS",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");
            _outputHelper.WriteLine($"Output Parameter 리스트:");
            _outputHelper.WriteLine($"Result: {parameters["Result"]}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");
            Assert.Equal(1, result.ReturnValue);

            // Dictionary에서 Output 파라미터 값 확인
            Assert.True(parameters.ContainsKey("Result"));
            Assert.Equal("홍길동님은 25살입니다.", parameters["Result"]);
        }

        #endregion

        #region 연결 테스트

        /// <summary>
        /// 데이터베이스 연결 테스트
        /// </summary>
        [Fact]
        public async Task TestConnection_ShouldSucceed()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();

            // Act
            var result = await dal.TestConnectionAsync(connectionInfo.ToConnectionString());

            // Assert
            Assert.True(result.IsSuccess, $"연결 실패: {result.Message}");
        }

        /// <summary>
        /// 잘못된 연결 정보로 연결 테스트 (실패 예상)
        /// </summary>
        [Fact]
        public async Task TestConnection_WithInvalidInfo_ShouldFail()
        {
            // Arrange
            var invalidConnectionInfo = new MsSqlConnectionInfo
            {
                Server = "invalid-server-name",
                Database = "InvalidDB",
                UserId = "invalid",
                Password = "invalid",
                IntegratedSecurity = false
            };
            var dal = GetTestDAL();

            // Act
            var result = await dal.TestConnectionAsync(invalidConnectionInfo.ToConnectionString());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Message));
        }

        #endregion

        #region DataSet 반환 테스트

        /// <summary>
        /// DataSet 반환 테스트
        /// </summary>
        [Fact]
        public async Task DataSetTest()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new SelectDataTestParam
            {
                Count = 2
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_SELECT_DATA",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");
            _outputHelper.WriteLine($"DataSet 리스트:");
            _outputHelper.WriteLine($"Tables: {result.DataSet.Tables.Count}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");
            Assert.NotNull(result.DataSet);
            Assert.True(result.DataSet.Tables.Count >= 1, "최소 1개의 테이블이 반환되어야 합니다.");
            Assert.Equal(2, result.ReturnValue);
        }

        /// <summary>
        /// Select Data 테스트용 파라미터 클래스
        /// </summary>
        public class SelectDataTestParam : SQLParam
        {
            [DbParameter("Count", DbType.Int32, ParameterDirection.Input)]
            public int Count { get; set; }
        }

        #endregion

        #region 복합 테스트 (Return + Output + DataSet)

        /// <summary>
        /// Return 값, Output 파라미터, DataSet 모두 반환하는 복합 테스트
        /// </summary>
        [Fact]
        public async Task ExecuteProcedure_Complex()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new ComplexTestParam
            {
                SearchKeyword = "테스트검색어"
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_COMPLEX",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");
            _outputHelper.WriteLine($"Output Parameter 리스트:");
            _outputHelper.WriteLine($"TotalCount: {parameters.TotalCount}");
            _outputHelper.WriteLine($"ProcessedAt: {parameters.ProcessedAt}");
            _outputHelper.WriteLine($"DataSet 리스트:");
            _outputHelper.WriteLine($"Tables: {result.DataSet.Tables.Count}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");

            // Return 값 확인
            Assert.Equal(0, result.ReturnValue);

            // Output 파라미터 확인
            Assert.Equal(100, parameters.TotalCount);
            Assert.NotEqual(default(DateTime), parameters.ProcessedAt);

            // DataSet 확인
            Assert.NotNull(result.DataSet);
            Assert.True(result.DataSet.Tables.Count > 0);
        }

        /// <summary>
        /// 복합 테스트용 파라미터 클래스
        /// </summary>
        public class ComplexTestParam : SQLParam
        {
            [DbParameter("SearchKeyword", DbType.String, ParameterDirection.Input)]
            public string? SearchKeyword { get; set; }

            [DbParameter("TotalCount", DbType.Int32, ParameterDirection.Output)]
            public int TotalCount { get; set; }

            [DbParameter("ProcessedAt", DbType.DateTime, ParameterDirection.Output)]
            public DateTime? ProcessedAt { get; set; }
        }

        #endregion

        #region Dynamic 파라미터 테스트 (IN/OUT 자동 매핑)

        /// <summary>
        /// DbParameter 속성에 Direction 지정 없이 이름만으로 IN/OUT 자동 매핑 테스트
        /// - 프로시저 메타데이터에서 Direction을 자동으로 가져옴
        /// - nullable 타입 사용으로 기본값 문제 방지
        /// </summary>
        [Fact]
        public async Task DynamicParamTest()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var parameters = new DynamicParam
            {
                Name = "홍길동",
                Age = 25
                // OUTPUT 파라미터는 초기값 설정 불필요 (null로 시작)
            };

            // Act
            var result = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TEST_DYNAMIC_PARAMS",
                parameters,
                isReturn: true
            );

            _outputHelper.WriteLine($"프로시저 실행 결과: {result.IsSuccess}, 반환값: {result.ReturnValue}");
            _outputHelper.WriteLine($"=== Input Parameters ===");
            _outputHelper.WriteLine($"Name: {parameters.Name}");
            _outputHelper.WriteLine($"Age: {parameters.Age}");
            _outputHelper.WriteLine($"=== Output Parameters (자동 매핑) ===");
            _outputHelper.WriteLine($"Greeting: {parameters.Greeting}");
            _outputHelper.WriteLine($"CalculatedAge: {parameters.CalculatedAge}");
            _outputHelper.WriteLine($"ProcessedAt: {parameters.ProcessedAt}");

            // Assert
            Assert.True(result.IsSuccess, $"프로시저 실행 실패: {result.Message}");
            Assert.Equal(0, result.ReturnValue);

            // Output 파라미터 자동 매핑 확인
            Assert.Equal("안녕하세요, 홍길동님! 25살이시군요.", parameters.Greeting);
            Assert.Equal(35, parameters.CalculatedAge); // 25 + 10 = 35
            Assert.NotNull(parameters.ProcessedAt);

            // DataSet 확인
            Assert.NotNull(result.DataSet);
            Assert.True(result.DataSet.Tables.Count > 0);
        }

        /// <summary>
        /// Dynamic 파라미터 테스트용 클래스
        /// - DbParameter 속성에 Direction을 지정하지 않음
        /// - 프로퍼티 이름과 프로시저 파라미터 이름이 일치하면 자동 매핑
        /// - nullable 타입 사용 권장 (DateTime 오류 방지)
        /// </summary>
        public class DynamicParam : SQLParam
        {
            // Input 파라미터
            [DbParameter]
            public string? Name { get; set; }

            [DbParameter]
            public int? Age { get; set; }

            // Output 파라미터 (Direction 지정 없이 자동 매핑)
            [DbParameter]
            public string? Greeting { get; set; }

            [DbParameter]
            public int? CalculatedAge { get; set; }

            [DbParameter]
            public DateTime? ProcessedAt { get; set; }
        }

        #endregion
    }
}
