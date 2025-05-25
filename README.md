# Eternal Warrior (가제)

## ✨ 프로젝트 개요

Unity 기반의 2D 로그라이크 RPG로, 플레이어가 다양한 스킬과 아이템을 조합해 맵에서 다수의 몬스터와 전투를 벌이는 게임입니다. 플레이어, 몬스터, 스킬, 아이템 등 주요 시스템을 모듈화하여 설계하였고, 커스텀 에디터 윈도우를 통해 데이터 생성·수정·저장 등 관리가 실시간으로 이루어집니다. 각 데이터는 Guid로 고유하게 식별되어 무결성과 중복 방지를 지원하며, 커스텀 A\* 길찾기, 계층적 데이터 직렬화, 런타임-에디터 연동 등 다양한 시스템을 직접 구현하여 유지보수와 확장성이 뛰어난 구조를 갖추고 있습니다.

---

## 🛠️ 주요 기술 및 구조

- **모듈형 시스템 설계**: 플레이어, 몬스터, 스킬, 아이템 등 핵심 기능을 독립적 모듈로 분리하여 유지보수성과 확장성을 극대화했습니다.
- **커스텀 에디터 툴**: Unity EditorWindow, EditorGUILayout, Foldout, HelpBox, GenericMenu 등 다양한 Editor API를 활용해, 아이템·스킬·드롭테이블 등 게임 데이터의 실시간 생성/수정/저장/동기화가 가능한 에디터를 직접 구현했습니다.
- **데이터 직렬화 및 무결성 관리**: 모든 데이터는 JSON/CSV로 직렬화하여 저장하며, Guid 기반 고유 식별자를 통해 중복 및 참조 오류를 방지합니다. 데이터 추가/삭제/초기화/저장 등 반복 작업은 버튼 클릭으로 처리할 수 있습니다.
- **런타임-에디터 연동**: 에디터에서 관리한 데이터를 런타임에서 역직렬화하여 활용하며, Dictionary<Guid, SkillData> 등 계층적 구조로 스킬·스탯·효과를 관리합니다.
- **커스텀 A\* 길찾기**: 직접 구현한 A\* 알고리즘 기반 경로 탐색 모듈을 통해, 몬스터 AI 및 이동 시스템의 유연성을 확보했습니다.
- **유틸리티 클래스 일원화**: SkillDataEditorUtility, ItemDataEditorUtility 등 static 유틸리티 클래스를 통해 데이터 저장/불러오기/삭제/초기화/무결성 검증 등 공통 로직을 일관성 있게 관리합니다.
- **트러블슈팅 및 안정성**: 데이터 중복, 참조 오류, 레이아웃 에러(GUILayout: Mismatched LayoutGroup) 등 문제를 try-finally, delayCall, 데이터 무결성 검증 등으로 해결하였으며, 실시간 동기화와 예외 안전성을 강화했습니다.

---

## 💡 코드 구조 예시

```csharp
// SkillSystem.cs (일부 발췌)
public class SkillSystem : MonoBehaviour, IInitializable
{
    private List<SkillData> availableSkills = new List<SkillData>();
    public void Initialize()
    {
        LoadSkillData();
    }
    private void LoadSkillData()
    {
        availableSkills = SkillDataManager.Instance.GetAllData();
    }
    public void AddOrUpgradeSkill(SkillData skillData)
    {
        // 기존 스킬이면 레벨업, 아니면 새로 추가
    }
}
```

```csharp
// SkillDataEditorUtility.cs (예시)
public static class SkillDataEditorUtility
{
    public static void SaveData(List<SkillData> dataList)
    {
        // JSON 직렬화 및 파일 저장
    }
    public static List<SkillData> LoadData()
    {
        // 파일에서 역직렬화
    }
    public static void ValidateData(List<SkillData> dataList)
    {
        // Guid 중복, 필수값 누락 등 무결성 검증
    }
}
```

---

## 📝 구현 및 설계 포인트

- 데이터 구조와 관리, 에디터-런타임 연동, 무결성 관리 등 실질적인 개발 경험을 강조할 수 있도록 설계하였습니다.
- 각종 반복 작업을 자동화하고, 실시간 동기화 및 예외 안전성을 확보하여 1인 개발 환경에서도 효율적으로 대규모 데이터를 관리할 수 있도록 했습니다.
- 스킬/스탯/효과/드롭테이블 등 데이터 구조와 관리 방식, 에디터-런타임 연동, 무결성 관리 등 구체적으로 기술하였습니다.

---
