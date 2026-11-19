# Chess

기존 3D 체스 모델로 즐기는 로컬 2인용 체스입니다.

## 실행

1. Unity Hub에서 이 폴더를 프로젝트로 추가합니다.
2. 프로젝트 기준 버전은 Unity 2023.1.22f1입니다.
3. `Assets/chess/Scenes/Chess.unity`를 열고 Play를 누릅니다.
4. 흰색이 먼저 시작합니다. 같은 컴퓨터에서 번갈아 조작합니다.

UI는 실행 시 자동으로 생성됩니다. 별도의 폰트 리소스나 Inspector 연결은 필요하지 않습니다.
기존 씬의 프리팹 연결은 유지했습니다.

## 조작

- 말을 누르면 합법적으로 이동할 수 있는 칸이 초록색으로 표시됩니다.
- 이동할 칸이나 상대 말을 누르면 이동합니다.
- 같은 말을 다시 누르거나 Esc를 누르면 선택을 취소합니다.
- 마지막 이동은 올리브색, 체크를 받은 킹의 칸은 붉은색으로 표시됩니다.
- Flip board 버튼 또는 F 키로 시점을 뒤집습니다.
- 폰이 끝줄에 도착하면 Queen, Rook, Bishop, Knight 버튼 또는 Q, R, B, N 키로 승급합니다.
- New game은 진행 중인 대국을 확인 후 초기화합니다.
- Resign은 현재 차례의 플레이어가 확인 후 기권합니다.
- 대국 종료 후 Play again으로 다시 시작할 수 있습니다.

## 규칙과 기록

일반 이동과 포획, 체크, 체크메이트, 스테일메이트, 캐슬링, 앙파상, 네 종류의 승급을 지원합니다.
자기 킹을 체크에 노출시키는 이동과 상대 킹을 직접 잡는 이동은 허용하지 않습니다.
승급을 선택하기 전에는 턴이 넘어가지 않습니다.

무승부 조건은 다음과 같습니다.

- 완료된 현재 포지션이 세 번 반복되거나 폰 이동과 포획 없이 50수가 진행되면 Claim draw 버튼이 표시됩니다.
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

이번 작업의 규칙 검사와 Windows 실행 검증은 별도 복사본에서 Unity 6000.3.17f1로 진행했습니다.
원본 프로젝트의 Unity 2023.1.22f1 설정은 유지했으며 해당 버전에서의 실행 검증은 하지 못했습니다.

## 커밋 형식

기존 이력에 맞춰 기능 추가는 `[추가]`, 동작 수정은 `[수정]`으로 시작하는 한국어 커밋 메시지를 사용합니다.
독립적으로 검토할 수 있는 작업 단위로 순서대로 커밋합니다.
