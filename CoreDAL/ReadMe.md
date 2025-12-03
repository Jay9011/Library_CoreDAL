# SECUiDEA.CoreDAL

다중 데이터베이스를 지원하는 **데이터 액세스 레이어(DAL)** 라이브러리입니다.  
저장 프로시저 실행을 위한 추상화 레이어를 제공하며, 파라미터 자동 매핑 기능을 지원합니다.

---

## 📋 주요 특징

- **.NET Standard 2.0** 타겟 - 다양한 .NET 프로젝트에서 사용 가능
- **저장 프로시저 실행** - Sync/Async 모두 지원
- **파라미터 자동 매핑** - 프로시저 메타데이터 기반 자동 매핑
- **다중 데이터베이스 지원** - 확장 가능한 아키텍처
- **OUTPUT 파라미터 지원** - 자동 값 반환 처리
- **다중 ResultSet 지원** - DataSet으로 모든 결과 수집

---

## 🗄️ 지원 데이터베이스

| 데이터베이스 | 상태 | 비고 |
|--------------|------|------|
| **SQL Server** | ✅ 완전 지원 | Microsoft.Data.SqlClient 사용 |
| **Oracle** | 📋 계획 | 추후 확장 예정 |
| **MySQL** | 📋 계획 | 추후 확장 예정 |
| **PostgreSQL** | 📋 계획 | 추후 확장 예정 |

---

## 🚀 빠른 시작

### 1. 연결 정보 설정

```csharp
using CoreDAL.Configuration;
using CoreDAL.Configuration.Models;

var connectionInfo = new MsSqlConnectionInfo
{
    Server = "localhost",
    Database = "MyDatabase",
    UserId = "sa",
    Password = "YourPassword",
    Port = 1433,
    IntegratedSecurity = false  // Windows 인증 시 true
};
```

### 2. DAL 인스턴스 가져오기

```csharp
using CoreDAL.Configuration;
using CoreDAL.DALs.Interface;

ICoreDAL dal = DbDALFactory.CreateCoreDal(DatabaseType.MSSQL);
```

### 3. 프로시저 실행

#### 방법 1: SQLParam 클래스 사용 (권장)

```csharp
using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;

// 파라미터 모델 정의
public class MyProcedureParam : SQLParam
{
    [DbParameter]
    public string? Name { get; set; }
    
    [DbParameter]
    public int? Age { get; set; }
    
    [DbParameter]
    public string? Result { get; set; }  // OUTPUT 파라미터
}

// 프로시저 실행
var param = new MyProcedureParam { Name = "홍길동", Age = 25 };
var result = await dal.ExecuteProcedureAsync(
    connectionInfo.ToConnectionString(),
    "USP_MY_PROCEDURE",
    param,
    isReturn: true
);

// 결과 확인
Console.WriteLine($"성공: {result.IsSuccess}");
Console.WriteLine($"Return 값: {result.ReturnValue}");
Console.WriteLine($"OUTPUT 결과: {param.Result}");  // 자동 매핑됨
```

#### 방법 2: Dictionary 사용

```csharp
var parameters = new Dictionary<string, object>
{
    { "Name", "홍길동" },
    { "Age", 25 }
};

var result = await dal.ExecuteProcedureAsync(
    connectionInfo.ToConnectionString(),
    "USP_MY_PROCEDURE",
    parameters,
    isReturn: true
);

// OUTPUT 파라미터는 Dictionary에 자동 추가됨
Console.WriteLine($"Result: {parameters["Result"]}");
```

---

## 📁 프로젝트 구조

```
CoreDAL/
├── Configuration/
│   ├── Interface/
│   │   ├── IDatabaseSetup.cs         # DB 설정 인터페이스
│   │   ├── IDatabaseSetupContainer.cs # 다중 DB 설정 컨테이너
│   │   └── IDbConnectionInfo.cs      # 연결 정보 인터페이스
│   ├── Models/
│   │   ├── MsSqlConnectionInfo.cs    # MSSQL 연결 정보
│   │   └── OracleConnectionInfo.cs   # Oracle 연결 정보
│   ├── DatabaseType.cs               # DB 타입 열거형
│   ├── DbConnectionFactory.cs        # 연결 정보 팩토리
│   └── DbDALFactory.cs               # DAL 팩토리
├── DALs/
│   ├── Interface/
│   │   └── ICoreDAL.cs               # 핵심 DAL 인터페이스
│   ├── SqlServerDAL.cs               # MSSQL 구현
│   └── OracleDAL.cs                  # Oracle 구현 (확장 예정)
├── ORM/
│   ├── Extensions/
│   │   ├── DbParameterAttribute.cs   # 파라미터 속성
│   │   └── SystemDataExtensions.cs   # DataTable 확장 메서드
│   ├── Handlers/
│   │   ├── SqlServerParameterHandler.cs  # MSSQL 파라미터 핸들러
│   │   └── OracleParameterHandler.cs     # Oracle 파라미터 핸들러
│   ├── Interfaces/
│   │   ├── IDbParameterHandler.cs    # 파라미터 핸들러 인터페이스
│   │   └── ISQLParam.cs              # SQL 파라미터 인터페이스
│   ├── DatabaseParameterProcessor.cs # 파라미터 처리 프로세서
│   ├── SQLParam.cs                   # 파라미터 기본 클래스
│   └── SQLResult.cs                  # 실행 결과 클래스
└── Consts.cs                         # 상수 정의
```

---

## 🔧 핵심 클래스

### ICoreDAL

데이터베이스 작업을 위한 핵심 인터페이스입니다.

```csharp
public interface ICoreDAL
{
    // 연결 테스트
    Task<SQLResult> TestConnectionAsync(string connectionString);
    
    // 프로시저 실행 (Sync/Async, ISQLParam/Dictionary)
    SQLResult ExecuteProcedure(string connectionString, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);
    Task<SQLResult> ExecuteProcedureAsync(string connectionString, string storedProcedureName, ISQLParam parameters = null, bool isReturn = true);
}
```

### SQLResult

프로시저 실행 결과를 담는 클래스입니다.

```csharp
public class SQLResult : IDisposable
{
    public DataSet DataSet { get; set; }     // 쿼리 결과
    public bool IsSuccess { get; set; }       // 성공 여부
    public string Message { get; set; }       // 메시지
    public int ReturnValue { get; set; }      // RETURN 값
}
```

### DbParameterAttribute

파라미터 매핑을 위한 속성입니다.

```csharp
// 기본 사용 (이름 자동 매핑)
[DbParameter]
public string? Name { get; set; }

// 상세 설정
[DbParameter("ParamName", DbType.String, ParameterDirection.Output)]
public string? Result { get; set; }
```

---

## ✨ 파라미터 자동 매핑

### 동작 방식

1. `SqlCommandBuilder.DeriveParameters()`로 프로시저의 **실제 파라미터 정보** 조회
2. 사용자 모델의 프로퍼티와 **이름으로 매핑** (대소문자 무시)
3. **Direction은 프로시저 메타데이터에서 자동으로 가져옴**

### 장점

- `DbParameterAttribute`에 Direction 지정 불필요
- 하나의 모델로 여러 프로시저 호출 가능
- 프로시저에 없는 프로퍼티는 자동 무시

### 권장 사항

```csharp
public class MyParam : SQLParam
{
    // ✅ nullable 타입 사용 권장 (기본값 문제 방지)
    [DbParameter]
    public int? Age { get; set; }
    
    [DbParameter]
    public DateTime? ProcessedAt { get; set; }  // DateTime은 반드시 nullable!
    
    // ⚠️ 값 타입은 기본값이 전달될 수 있음
    [DbParameter]
    public int Count { get; set; }  // 0이 전달됨
}
```

---

## 🏗️ 아키텍처

### 디자인 패턴

| 패턴 | 적용 위치 | 설명 |
|------|-----------|------|
| **Factory** | `DbDALFactory`, `DbConnectionFactory` | 객체 생성 캡슐화 |
| **Singleton** | `SqlServerDAL`, `OracleDAL` | `Lazy<T>`로 스레드 안전 |
| **Strategy** | `IDbParameterHandler` 구현체 | DB별 파라미터 처리 분리 |
| **Result** | `SQLResult` | 성공/실패 결과 캡슐화 |

### SOLID 원칙

- **S**: 각 클래스가 단일 책임 담당
- **O**: 인터페이스 기반으로 확장 가능
- **L**: 구현체가 인터페이스 계약 준수
- **I**: 적절한 크기의 인터페이스 분리
- **D**: 추상화(인터페이스)에 의존

---

## 📦 의존성

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.3" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

---

## 🔌 새 데이터베이스 추가 방법

### 1. DatabaseType enum 추가

```csharp
public enum DatabaseType
{
    MSSQL,
    ORACLE,
    MYSQL,  // 추가
}
```

### 2. 구현 클래스 생성

- `MySqlDAL : ICoreDAL`
- `MySqlParameterHandler : IDbParameterHandler`
- `MySqlConnectionInfo : IDbConnectionInfo`

### 3. Factory switch 문 업데이트

- `DbDALFactory.CreateNewInstance()`
- `DbConnectionFactory.CreateConnectionInfo()`
- `DatabaseParameterProcessor.CreateParameterHandler()`

---