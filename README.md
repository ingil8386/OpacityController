
# OpacityController
반드시 관리자권한으로 실행

실행파일 경로: WindowOpacityController\WindowOpacityController\bin\Debug\net10.0-windows\WindowOpacityContr

Windows에서 실행 중인 창 목록을 불러오고, 선택한 창의 투명도를 조절하거나 단축키로 창을 숨길 수 있는 WPF 기반 데스크톱 프로그램입니다.

## 프로젝트 소개

OpacityController는 Windows API를 활용하여 현재 실행 중인 프로그램 창을 제어하는 실습용 프로젝트입니다.  
창 목록을 조회한 뒤 원하는 창을 선택하고, 슬라이더를 이용해 해당 창의 투명도를 조절할 수 있습니다.

또한 전역 단축키를 등록하여 선택한 창 또는 OpacityController 프로그램 자체를 빠르게 숨기고 다시 표시할 수 있습니다.

## 주요 기능

- 실행 중인 Windows 창 목록 조회
- 선택한 창의 투명도 조절
- 선택한 창 숨기기 / 다시 보이기
- OpacityController 프로그램 숨기기 / 다시 보이기
- 사용자 지정 UI 적용
- Windows API 기반 창 제어

## 단축키

| 단축키 | 기능 |
|---|---|
| Ctrl + ] | 선택한 창 숨기기 / 다시 보이기 |
| Ctrl + [ | OpacityController 프로그램 숨기기 / 다시 보이기 |

## 개발 환경

| 항목 | 내용 |
|---|---|
| OS | Windows |
| Language | C# |
| Framework | .NET 10.0 |
| UI | WPF |
| IDE | Visual Studio |
| Project Type | Windows Desktop Application |

## 사용 기술

- C#
- WPF
- XAML
- .NET
- Windows API
- P/Invoke
- user32.dll

## 프로젝트 구조

```text
OpacityController
├── README.md
└── WindowOpacityController
    ├── WindowOpacityController.slnx
    └── WindowOpacityController
        ├── App.xaml
        ├── App.xaml.cs
        ├── MainWindow.xaml
        ├── MainWindow.xaml.cs
        └── WindowOpacityController.csproj
