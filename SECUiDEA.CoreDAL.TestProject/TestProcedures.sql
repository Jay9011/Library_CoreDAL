-- ============================================
-- SECUiDEA.CoreDAL 테스트용 프로시저 스크립트
-- SQL Server에서 실행하여 테스트 프로시저를 생성하세요.
-- ============================================

-- 1. Return Value 테스트용 프로시저
-- 입력값의 2배를 RETURN으로 반환
IF OBJECT_ID('dbo.USP_TEST_RETURN_VALUE', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_RETURN_VALUE
GO

CREATE PROCEDURE [dbo].[USP_TEST_RETURN_VALUE]
    @InputValue INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Return 값으로 입력값의 2배 반환
    RETURN @InputValue * 2
END
GO

-- ============================================

-- 2. Output Parameter 테스트용 프로시저
-- Output 파라미터로 계산 결과와 메시지 반환
IF OBJECT_ID('dbo.USP_TEST_OUTPUT_PARAMS', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_OUTPUT_PARAMS
GO

CREATE PROCEDURE [dbo].[USP_TEST_OUTPUT_PARAMS]
    @InputValue INT,
    @OutputValue INT OUTPUT,
    @OutputMessage NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @OutputValue = @InputValue * 3
    SET @OutputMessage = N'처리 완료: ' + CAST(@InputValue AS NVARCHAR(10))
    
    RETURN 0
END
GO

-- ============================================

-- 3. Dictionary 파라미터 테스트용 프로시저
IF OBJECT_ID('dbo.USP_TEST_DICTIONARY_PARAMS', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_DICTIONARY_PARAMS
GO

CREATE PROCEDURE [dbo].[USP_TEST_DICTIONARY_PARAMS]
    @Name NVARCHAR(50),
    @Age INT,
    @Result NVARCHAR(100) OUTPUT,
    @OutputValue INT OUTPUT,
    @OutputMessage NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @OutputValue = 0000;
    SET @OutputMessage = 'SUCCESS';
    
    SET @Result = @Name + N'님은 ' + CAST(@Age AS NVARCHAR(10)) + N'살입니다.'
    
    RETURN 1
END
GO

-- ============================================

-- 4. DataSet 반환 테스트용 프로시저
-- 다중 ResultSet 반환
IF OBJECT_ID('dbo.USP_TEST_SELECT_DATA', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_SELECT_DATA
GO

CREATE PROCEDURE [dbo].[USP_TEST_SELECT_DATA]
    @Count INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 첫 번째 ResultSet (최대 @Count개 반환)
    SELECT TOP (@Count) *
    FROM (
        SELECT 1 AS Id, N'테스트1' AS Name
        UNION ALL
        SELECT 2, N'테스트2'
        UNION ALL
        SELECT 3, N'테스트3'
    ) AS T
    
    -- 두 번째 ResultSet
    SELECT GETDATE() AS CurrentTime, @@VERSION AS ServerVersion
    
    RETURN @Count
END
GO

-- ============================================

-- 5. 복합 테스트용 프로시저 (Return + Output + DataSet)
IF OBJECT_ID('dbo.USP_TEST_COMPLEX', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_COMPLEX
GO

CREATE PROCEDURE [dbo].[USP_TEST_COMPLEX]
    @SearchKeyword NVARCHAR(50),
    @TotalCount INT OUTPUT,
    @ProcessedAt DATETIME OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Output 파라미터 설정
    SET @TotalCount = 100
    SET @ProcessedAt = GETDATE()
    
    -- 검색 결과 반환 (예시)
    SELECT 1 AS Id, @SearchKeyword AS Keyword, N'결과1' AS Result
    UNION ALL
    SELECT 2, @SearchKeyword, N'결과2'
    
    -- 성공 시 0 반환
    RETURN 0
END
GO

-- ============================================

-- 6. Dynamic 파라미터 테스트용 프로시저 (IN/OUT 혼합)
-- DbParameter 속성에 Direction 지정 없이 이름만으로 자동 매핑 테스트
IF OBJECT_ID('dbo.USP_TEST_DYNAMIC_PARAMS', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TEST_DYNAMIC_PARAMS
GO

CREATE PROCEDURE [dbo].[USP_TEST_DYNAMIC_PARAMS]
    @Name NVARCHAR(50),                    -- IN: 이름
    @Age INT,                              -- IN: 나이
    @Greeting NVARCHAR(200) OUTPUT,        -- OUT: 인사말
    @CalculatedAge INT OUTPUT,             -- OUT: 계산된 나이 (10년 후)
    @ProcessedAt DATETIME OUTPUT           -- OUT: 처리 시간
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Output 파라미터 설정
    SET @Greeting = N'안녕하세요, ' + @Name + N'님! ' + CAST(@Age AS NVARCHAR(10)) + N'살이시군요.'
    SET @CalculatedAge = @Age + 10
    SET @ProcessedAt = GETDATE()
    
    -- 결과 데이터 반환
    SELECT @Name AS Name, @Age AS Age, @CalculatedAge AS AgeAfter10Years
    
    RETURN 0
END
GO

-- ============================================

-- 7. 트랜잭션 테스트용 테이블 및 프로시저
-- 트랜잭션 롤백 테스트를 위한 임시 테이블 및 INSERT 프로시저

-- 테스트용 임시 테이블 생성 (존재하지 않을 경우)
IF OBJECT_ID('dbo.TBL_TX_TEST', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TBL_TX_TEST (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    )
END
GO

-- 트랜잭션 테스트용 INSERT 프로시저
IF OBJECT_ID('dbo.USP_TX_TEST_INSERT', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TX_TEST_INSERT
GO

CREATE PROCEDURE [dbo].[USP_TX_TEST_INSERT]
    @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.TBL_TX_TEST (Name) VALUES (@Name)
    
    RETURN 0
END
GO

-- 트랜잭션 테스트용 SELECT 프로시저 (데이터 확인용)
IF OBJECT_ID('dbo.USP_TX_TEST_SELECT', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TX_TEST_SELECT
GO

CREATE PROCEDURE [dbo].[USP_TX_TEST_SELECT]
    @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT Id, Name, CreatedAt 
    FROM dbo.TBL_TX_TEST 
    WHERE Name = @Name
    
    RETURN 0
END
GO

-- 트랜잭션 테스트용 DELETE 프로시저 (정리용)
IF OBJECT_ID('dbo.USP_TX_TEST_DELETE', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TX_TEST_DELETE
GO

CREATE PROCEDURE [dbo].[USP_TX_TEST_DELETE]
    @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.TBL_TX_TEST WHERE Name = @Name
    
    RETURN @@ROWCOUNT
END
GO

-- 트랜잭션 테스트용 실패 프로시저 (의도적으로 에러 발생)
IF OBJECT_ID('dbo.USP_TX_TEST_FAIL', 'P') IS NOT NULL
    DROP PROCEDURE dbo.USP_TX_TEST_FAIL
GO

CREATE PROCEDURE [dbo].[USP_TX_TEST_FAIL]
    @ShouldFail BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @ShouldFail = 1
    BEGIN
        RAISERROR('Intentional error for transaction rollback test', 16, 1)
        RETURN -1
    END
    
    RETURN 0
END
GO

-- ============================================
-- 테스트 프로시저 생성 완료
-- ============================================
PRINT '모든 테스트 프로시저가 생성되었습니다.'
GO

