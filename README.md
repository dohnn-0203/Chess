# Chess

기존 3D 체스 모델로 즐기는 로컬 2인용 체스입니다.

## 실행

1. Unity Hub에서 이 폴더를 프로젝트로 추가합니다.
2. 프로젝트 기준 버전은 Unity 2023.1.22f1입니다.
3. `Assets/chess/Scenes/Chess.unity`를 열고 Play를 누릅니다.
4. 흰색이 먼저 시작합니다. 같은 컴퓨터에서 번갈아 조작합니다.

기존 씬의 로그 UI와 카메라 구도, 배경을 사용합니다. 최근 로그 3줄은 원래 글꼴과 투명도로 표시됩니다.
원본 UI가 참조하는 TextMesh Pro 기본 글꼴과 필수 리소스를 포함했습니다. 씬과 프리팹 연결은 유지했습니다.
오른쪽 고정 패널 대신 필요한 기능만 Menu에서 열 수 있습니다.
원근과 카메라 각도는 유지하고 화면 비율에 맞춰 거리를 조절해 모든 기물이 보이게 합니다.

## 조작

- 말을 누르면 합법적으로 이동할 수 있는 칸이 초록색으로 표시됩니다.
- 이동할 칸이나 상대 말을 누르면 이동합니다.
- 같은 말을 다시 누르거나 Esc를 누르면 선택을 취소합니다.
- 마지막 이동은 올리브색, 체크를 받은 킹의 칸은 붉은색으로 표시됩니다.
- Menu에서 전체 이동 기록과 잡은 말을 확인하고 재시작, 기권, 시점 전환을 선택합니다. Esc로 메뉴를 닫습니다.
- Menu의 Flip board 버튼 또는 F 키로 시점을 뒤집습니다.
- 폰이 끝줄에 도착하면 Queen, Rook, Bishop, Knight 버튼 또는 Q, R, B, N 키로 승급합니다.
- New game은 진행 중인 대국을 확인 후 초기화합니다.
- Resign은 현재 차례의 플레이어가 확인 후 기권합니다.
- 대국 종료 후 Play again으로 다시 시작할 수 있습니다.

## 규칙과 기록

일반 이동과 포획, 체크, 체크메이트, 스테일메이트, 캐슬링, 앙파상, 네 종류의 승급을 지원합니다.
자기 킹을 체크에 노출시키는 이동과 상대 킹을 직접 잡는 이동은 허용하지 않습니다.
승급을 선택하기 전에는 턴이 넘어가지 않습니다.

무승부 조건은 다음과 같습니다.

- 완료된 현재 포지션이 세 번 반복되거나 폰 이동과 포획 없이 50수가 진행되면 Menu에 Claim draw 버튼이 표시됩니다.
- 같은 포지션이 다섯 번 반복되거나 폰 이동과 포획 없이 75수가 진행되면 자동 종료됩니다.
- 킹만 남음, 킹과 비숍 대 킹, 킹과 나이트 대 킹, 같은 색 칸의 비숍만 남은 경우를 기물 부족으로 판정합니다.
- 반복 판정에는 차례, 캐슬링 권리, 실제로 가능한 앙파상을 포함합니다.
- 마지막 수가 체크메이트인 경우 75수 무승부보다 체크메이트가 우선합니다.

이동 기록은 출발 칸과 도착 칸을 함께 표시합니다. 예: `e2-e4`, `Ng1-f3`, `e4xd5`.
캐슬링은 `O-O` 또는 `O-O-O`, 승급은 `=Q` 등의 접미사, 체크는 `+`, 체크메이트는 `#`로 표시됩니다.
잡은 말은 해당 팀이 포획한 상대 기물의 문자로 표시됩니다.

## Windows 빌드

Build Settings에 Chess 씬이 등록되어 있습니다. Windows 64-bit를 선택해 빌드할 수 있습니다.
기본 창 크기는 1280 x 720입니다. 1024 x 768 이상의 가로 화면을 권장합니다.

명령줄 빌드 예시:

```powershell
& $UnityEditor -batchmode -quit -projectPath $ProjectPath -executeMethod ChessBuild.Windows -chessBuildPath $ExePath -logFile $BuildLog
```

## WebGL 빌드와 포트폴리오 삽입

사용할 Unity 버전에 WebGL Build Support 모듈을 설치한 뒤 다음 명령을 실행합니다.
`$WebGLPath`에는 실행 파일 이름 대신 빌드 결과를 저장할 폴더의 절대 경로를 지정합니다.

```powershell
& $UnityEditor -batchmode -quit -buildTarget WebGL -projectPath $ProjectPath -executeMethod ChessBuild.WebGL -chessBuildPath $WebGLPath -logFile $BuildLog
```

이 빌드는 GitHub Pages에서 사용할 수 있도록 Gzip 압축과 Decompression Fallback을 함께 적용합니다.
압축 파일의 `.unityweb` 확장자와 로더가 참조하는 상대 경로를 그대로 유지해야 합니다.
서버에서 파일별 `Content-Encoding`을 설정하지 않아도 Unity 로더가 압축을 해제합니다.
브라우저에서 별도의 스레드나 교차 출처 격리 헤더를 요구하지 않도록 WebGL 스레드는 사용하지 않습니다.
빌드가 끝나거나 실패하면 편집기의 기존 WebGL 설정으로 복원합니다.

새 정적 사이트에는 빌드 폴더 전체를 복사하고, 페이지에서 다음과 같은 iframe으로 표시할 수 있습니다.

```html
<iframe src="games/chess/" title="체스 게임" allow="fullscreen" allowfullscreen></iframe>
```

현재 포트폴리오의 플레이 위치는 [체스 프로젝트 영역](https://dohnn-0203.github.io/#chess)입니다.
포트폴리오의 `games/chess/index.html`, `player.js`, `player.css`는 로딩 상태와 오류 표시를 위한 전용 플레이어입니다.
게임을 다시 빌드할 때는 이 파일들을 Unity가 생성한 기본 페이지로 덮어쓰지 않고, 새 `Build/` 파일만 교체합니다.
그다음 `games/chess/build-config.json`의 `loaderUrl`, `dataUrl`, `frameworkUrl`, `codeUrl`을 새 빌드 파일 이름에 맞게 갱신합니다.

페이지 이동 없이 실행하려면 시작 버튼을 눌렀을 때만 iframe을 생성하고, 종료 시에는 iframe을 제거합니다.
이렇게 하면 포트폴리오를 처음 열 때 게임 파일을 내려받지 않고, 종료 후 게임이 사용하는 메모리도 해제할 수 있습니다.
로컬 확인은 파일을 직접 여는 대신 HTTP 서버로 제공해야 하며, 실제 배포 후에는 브라우저에서 로딩과 입력을 확인합니다.

## 검증

규칙 검증은 아래 명령으로 실행합니다. `$UnityEditor`에는 Unity.exe의 절대 경로를 지정합니다.

```powershell
& $UnityEditor -batchmode -nographics -projectPath $ProjectPath -executeMethod ChessValidation.RunRules -chessValidationResults $ReportPath -logFile $TestLog
```

초기 포지션의 깊이 1, 2, 3 합법 수 개수인 20, 400, 8902와 핀, 캐슬링, 앙파상, 체크메이트, 무승부, 승급을 검증합니다.
결과는 JSON으로 저장되며 실패 시 종료 코드 1을 반환합니다.

실제 플레이와 UI 검증은 Development Build에서 사용할 수 있습니다.

```powershell
& $UnityEditor -batchmode -quit -projectPath $ProjectPath -executeMethod ChessBuild.Windows -chessDevelopment -chessBuildPath $ExePath -logFile $BuildLog
& $ExePath -chessSmokeTest $SmokeOutputPath -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile $PlayerLog
```

검증 옵션을 주지 않으면 일반 게임으로 실행됩니다.
숨긴 Windows 창에서 화면 캡처가 검게 저장되는 환경은 `-chessSkipScreenshots`로 캡처만 제외하고 기능 검사를 실행할 수 있습니다.
이 경우 결과 JSON에 `screenshotsSkipped`를 기록하며, 실제 렌더링과 마우스 입력은 별도로 브라우저에서 확인해야 합니다.

이번 작업의 규칙 검사와 Windows 실행 검증은 별도 복사본에서 Unity 6000.3.17f1로 진행했습니다.
원본 프로젝트의 Unity 2023.1.22f1 설정은 유지했으며 해당 버전에서의 실행 검증은 하지 못했습니다.

WebGL 빌드는 WebGL Build Support가 설치된 Unity 2022.3.62f3의 별도 복사본에서 성공했습니다.
이 빌드 복사본의 패키지 구성은 Unity 내장 모듈, TextMeshPro 3.0.6, UGUI 1.0.0으로 제한했습니다.
원본 저장소의 Unity 버전과 패키지 설정은 유지했습니다. WebGL 빌드 성공과 실제 공개 페이지에서의 실행 검증은 구분합니다.

## 커밋 형식

기존 이력에 맞춰 기능 추가는 `[추가]`, 동작 수정은 `[수정]`으로 시작하는 한국어 커밋 메시지를 사용합니다.
독립적으로 검토할 수 있는 작업 단위로 순서대로 커밋합니다.
