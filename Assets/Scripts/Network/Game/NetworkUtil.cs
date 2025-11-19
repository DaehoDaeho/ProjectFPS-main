using System.Collections.Generic;

/// <summary>
/// NetworkRunner의 플레이어 목록 스냅샷을 가져오는 간단 유틸.
/// players 딕셔너리가 private이라면,
/// 필요 시 NetworkRunner에 공개 getter를 추가해도 된다.
/// 여기선 편의상 '호스트 0 + 클라 1..N' 이 있다고 가정한다.
/// </summary>
public static class NetworkUtil
{
    public static List<int> GetCurrentPlayerIds()
    {
        // 데모용: 호스트 id=0, 클라 id는 1..N이라고 가정
        // 실제로는 NetworkRunner에 공개 API를 추가해 안전하게 조회할 것을 권장.
        List<int> ids = new List<int>();
        ids.Add(0); // 호스트도 플레이어로 포함
        // 간단화를 위해 1~4까지 미리 예약(실전이라면 Runner에서 실제 접속자 조회)
        ids.Add(1);
        ids.Add(2);
        ids.Add(3);
        ids.Add(4);
        return ids;
    }
}
