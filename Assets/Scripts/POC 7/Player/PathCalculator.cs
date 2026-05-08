using System.Collections.Generic;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 링 벽과 장애물을 모두 고려하여 플레이어의 반사 경로 경유 지점을 계산하는 정적 유틸리티 클래스.
    /// PlayerController와 AttackPathIndicator 양쪽에서 공유한다.
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
        /// [설계]
        /// bounceCount + 1번의 기본 스텝을 실행하며, 각 스텝은 장애물 또는 링 벽 중
        /// 먼저 만나는 표면을 처리한다. 기본 스텝이 끝난 뒤 마지막 지점이 장애물이면
        /// 링 벽에 닿을 때까지 계속 연장한다(장애물 감지 포함). 이로써 경로는 항상
        /// 링 벽에서 끝나고, 장애물을 연속으로 여러 번 튕겨도 모두 정확하게 처리된다.
        /// </summary>
        /// <param name="startPos">경로 시작 위치 (world space).</param>
        /// <param name="direction">출발 방향 단위벡터.</param>
        /// <param name="ringCenter">링 중심 위치 (world space).</param>
        /// <param name="ringRadius">링 내벽 반경.</param>
        /// <param name="bounceCount">기본 반사 횟수. 총 (bounceCount + 1)번의 기본 스텝이 실행된다.</param>
        /// <param name="obstacleLayerMask">장애물 감지에 사용할 레이어 마스크.</param>
        public static WaypointInfo[] ComputeWaypoints(
            Vector2 startPos,
            Vector2 direction,
            Vector2 ringCenter,
            float ringRadius,
            int bounceCount,
            LayerMask obstacleLayerMask)
        {
            var result = new List<WaypointInfo>();
            Vector2 pos = startPos;
            Vector2 dir = direction;

            // 기본 스텝: bounceCount + 1번 실행, 각 스텝에서 장애물 또는 링 벽을 처리한다.
            for (int i = 0; i <= bounceCount; i++)
            {
                result.Add(ComputeNextWaypoint(ref pos, ref dir, ringCenter, ringRadius, obstacleLayerMask));
            }

            // 마지막 지점이 장애물이면 링 벽에 닿을 때까지 경로를 연장한다.
            // 장애물을 연속으로 여러 번 만나는 경우에도 모두 감지하여 올바르게 처리한다.
            const int maxExtraSteps = 20;
            for (int extra = 0; extra < maxExtraSteps; extra++)
            {
                if (result[result.Count - 1].HitObstacle == null)
                    break;

                result.Add(ComputeNextWaypoint(ref pos, ref dir, ringCenter, ringRadius, obstacleLayerMask));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 현재 위치(pos)에서 방향(dir)으로 다음 충돌 지점(장애물 또는 링 벽)을 하나 계산하고
        /// pos와 dir을 반사 후 값으로 갱신한다.
        ///
        /// ref 파라미터로 pos와 dir을 직접 수정하므로 다음 스텝에 즉시 연결된다.
        /// </summary>
        private static WaypointInfo ComputeNextWaypoint(
            ref Vector2 pos,
            ref Vector2 dir,
            Vector2 ringCenter,
            float ringRadius,
            LayerMask obstacleLayerMask)
        {
            float ringDist = GetRingIntersectionDistance(pos, dir, ringCenter, ringRadius);

            // 유효한 링 교점 없음: 현재 위치를 그대로 반환하고 갱신하지 않는다.
            if (ringDist < 0.1f)
                return new WaypointInfo { Position = pos, HitObstacle = null };

            // 링 벽까지의 경로 안에 장애물이 있는지 레이캐스트로 검사한다.
            RaycastHit2D obstacleHit = Physics2D.Raycast(pos, dir, ringDist, obstacleLayerMask);

            Vector2 nextPos;
            Vector2 reflectNormal;
            Obstacle hitObstacle = null;

            if (obstacleHit.collider != null && obstacleHit.collider.TryGetComponent(out Obstacle obs))
            {
                // 장애물이 링 벽보다 가까움: 장애물 표면 법선으로 반사한다.
                // 동일 장애물 재충돌 방지를 위해 법선 방향으로 소량 오프셋한다.
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

            // pos와 dir을 ref로 갱신하여 다음 ComputeNextWaypoint 호출에 이어진다.
            pos = nextPos;
            dir = Vector2.Reflect(dir, reflectNormal);

            return new WaypointInfo { Position = nextPos, HitObstacle = hitObstacle };
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
