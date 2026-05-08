using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 링 벽과 장애물을 모두 고려하여 플레이어의 반사 경로 경유 지점을 계산하는 정적 유틸리티 클래스.
    /// PlayerController와 AttackPathIndicator 양쪽에서 공유하여 중복 구현을 제거한다.
    /// </summary>
    public static class PathCalculator
    {
        /// <summary>
        /// 경로 상의 각 반사 지점에 대한 정보를 담는 구조체.
        /// </summary>
        public struct WaypointInfo
        {
            /// <summary>반사 지점의 월드 좌표.</summary>
            public Vector2 Position;

            /// <summary>이 지점에서 충돌한 장애물. 링 벽에 반사된 경우 null.</summary>
            public Obstacle HitObstacle;
        }

        /// <summary>
        /// 시작 위치와 방향을 받아 링 벽과 장애물을 모두 고려한 반사 경로의 경유 지점 목록을 계산한다.
        ///
        /// 각 반사 단계의 처리 흐름:
        /// 1. 2차 방정식으로 링 벽까지의 거리를 구한다 (출발점이 링 위가 아닌 경우도 처리 가능).
        /// 2. 그 거리 안에서 obstacleLayerMask 레이어로 레이캐스트한다.
        /// 3. 장애물이 링 벽보다 가까우면 장애물 표면 법선으로, 아니면 링 벽 법선으로 반사한다.
        ///
        /// [중요] 마지막 경유 지점이 장애물인 경우:
        /// 반사 방향으로 링 벽까지 세그먼트를 한 개 더 추가한다.
        /// 플레이어는 항상 링 벽에서 착지해야 하므로, 장애물이 bounce budget 마지막에 있어도
        /// 올바른 반사 경로로 링 벽까지 도달한다.
        /// </summary>
        /// <param name="startPos">경로 시작 위치 (world space).</param>
        /// <param name="direction">출발 방향 단위벡터.</param>
        /// <param name="ringCenter">링 중심 위치 (world space).</param>
        /// <param name="ringRadius">링 내벽 반경.</param>
        /// <param name="bounceCount">반사 횟수. 총 (bounceCount + 1)개의 기본 경유 지점이 생성된다.</param>
        /// <param name="obstacleLayerMask">장애물 감지에 사용할 레이어 마스크.</param>
        public static WaypointInfo[] ComputeWaypoints(
            Vector2 startPos,
            Vector2 direction,
            Vector2 ringCenter,
            float ringRadius,
            int bounceCount,
            LayerMask obstacleLayerMask)
        {
            int totalPoints = bounceCount + 1;
            var waypoints = new WaypointInfo[totalPoints];

            Vector2 pos = startPos;
            Vector2 dir = direction;

            for (int i = 0; i < totalPoints; i++)
            {
                float ringDist = GetRingIntersectionDistance(pos, dir, ringCenter, ringRadius);

                if (ringDist < 0.1f)
                {
                    // 유효한 교점 없음: 남은 지점을 모두 현재 위치로 채우고 종료한다.
                    for (int j = i; j < totalPoints; j++)
                        waypoints[j] = new WaypointInfo { Position = pos, HitObstacle = null };
                    break;
                }

                // 링 벽까지의 경로 안에 장애물이 있는지 레이캐스트로 검사한다.
                RaycastHit2D obstacleHit = Physics2D.Raycast(pos, dir, ringDist, obstacleLayerMask);

                Vector2 nextPos;
                Vector2 reflectNormal;
                Obstacle hitObstacle = null;

                if (obstacleHit.collider != null && obstacleHit.collider.TryGetComponent(out Obstacle obs))
                {
                    // 장애물이 링 벽보다 가까움: 장애물 표면 법선으로 반사한다.
                    // 동일 장애물에 재충돌하지 않도록 법선 방향으로 소량 오프셋한다.
                    nextPos = obstacleHit.point + obstacleHit.normal * 0.02f;
                    reflectNormal = obstacleHit.normal;
                    hitObstacle = obs;
                }
                else
                {
                    // 링 벽에 도달: 교점의 외향 법선(링 중심 → 교점 방향)으로 반사한다.
                    nextPos = pos + ringDist * dir;
                    reflectNormal = (nextPos - ringCenter).normalized;
                }

                waypoints[i] = new WaypointInfo { Position = nextPos, HitObstacle = hitObstacle };

                // 마지막 스텝이어도 dir과 pos를 갱신한다.
                // 마지막 지점이 장애물일 때 추가 링 벽 스텝 계산에 최신 값이 필요하다.
                dir = Vector2.Reflect(dir, reflectNormal);
                pos = nextPos;
            }

            // 마지막 경유 지점이 장애물이면 반사 방향으로 링 벽까지 한 세그먼트를 추가한다.
            // 이를 생략하면 플레이어가 장애물 위치 기준으로 링 벽에 착지하여 방향이 틀어진다.
            WaypointInfo last = waypoints[totalPoints - 1];
            if (last.HitObstacle != null)
            {
                float extraDist = GetRingIntersectionDistance(last.Position, dir, ringCenter, ringRadius);
                if (extraDist > 0.1f)
                {
                    var extended = new WaypointInfo[totalPoints + 1];
                    System.Array.Copy(waypoints, extended, totalPoints);
                    extended[totalPoints] = new WaypointInfo
                    {
                        Position = last.Position + dir * extraDist,
                        HitObstacle = null
                    };
                    return extended;
                }
            }

            return waypoints;
        }

        /// <summary>
        /// pos에서 dir 방향으로 나아갈 때 반경 ringRadius인 원(중심 ringCenter)과의 전방 교점까지의 거리를 반환한다.
        ///
        /// 직선과 원의 방정식을 연립하면 아래 2차 방정식이 나온다 (v = pos - ringCenter):
        ///   t^2 + 2*(v·d)*t + (|v|^2 - r^2) = 0
        ///
        /// 전방 교점으로 t2 = (-b + sqrt(b^2 - 4c)) / 2를 선택한다.
        /// pos가 링 위에 있는 경우와 링 내부에 있는 경우(장애물 반사 후) 모두 올바르게 동작한다.
        /// </summary>
        /// <returns>전방 교점까지의 거리. 유효하지 않으면 -1.</returns>
        private static float GetRingIntersectionDistance(Vector2 pos, Vector2 dir, Vector2 ringCenter, float ringRadius)
        {
            Vector2 v = pos - ringCenter;
            float b = 2f * Vector2.Dot(v, dir);
            float c = Vector2.Dot(v, v) - ringRadius * ringRadius;
            float discriminant = b * b - 4f * c;

            if (discriminant < 0f)
                return -1f;

            // t2는 두 해 중 항상 큰 값이므로 전방(양수) 교점에 해당한다.
            float t2 = (-b + Mathf.Sqrt(discriminant)) * 0.5f;
            return t2 > 0.1f ? t2 : -1f;
        }
    }
}
