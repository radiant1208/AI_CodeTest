using System;
using System.Collections.Generic;
using System.Drawing;
using PathSearch.Map;

namespace PathSearch.Planning.Heuristics
{
    /// <summary>회전 제약을 무시한 point-robot 근사 2D A*(역방향 Dijkstra) 기반 장애물 회피 휴리스틱.
    /// 생성 시 Goal 기준 전체 맵의 Distance Map(px)을 한 번만 계산해 O(1) 조회를 제공한다.</summary>
    public sealed class HolonomicObstacleHeuristic : IHeuristic
    {
        private static readonly (int Dx, int Dy, double Cost)[] Neighbors =
        {
            (-1, -1, Math.Sqrt(2)), (0, -1, 1.0), (1, -1, Math.Sqrt(2)),
            (-1, 0, 1.0),                          (1, 0, 1.0),
            (-1, 1, Math.Sqrt(2)),  (0, 1, 1.0),   (1, 1, Math.Sqrt(2)),
        };

        private readonly double[] _distance;
        private readonly int _width;
        private readonly int _height;

        /// <summary>ObstacleInflator로 robotRadius만큼 부풀린 격자에서 goal 기준 전체 Distance Map(px)을 사전 계산한다.</summary>
        public HolonomicObstacleHeuristic(OccupancyGrid grid, Point goal, double robotRadius)
        {
            ArgumentNullException.ThrowIfNull(grid);

            _width = grid.Width;
            _height = grid.Height;

            OccupancyGrid inflated = ObstacleInflator.Inflate(grid, robotRadius);
            _distance = ComputeDistanceMap(inflated, goal);
        }

        /// <summary>격자 좌표 (x,y)에서 goal까지 사전 계산된 8방향 거리(px). 도달 불가능하면 double.MaxValue.</summary>
        public double GetDistance(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return double.MaxValue;
            }

            return _distance[y * _width + x];
        }

        /// <summary>연속 좌표를 가장 가까운 격자 좌표로 반올림해 GetDistance를 조회한다(heading은 사용하지 않음).</summary>
        public double Estimate(double x, double y, double headingRad)
        {
            return GetDistance((int)Math.Round(x), (int)Math.Round(y));
        }

        // goal을 단일 시작점으로 한 Dijkstra(8방향 가중치 이동)로 전체 맵의 거리를 확산시킨다.
        // PriorityQueue<TElement,TPriority>는 decrease-key를 지원하지 않으므로, 더 짧은 거리가 발견될 때마다
        // 중복 enqueue를 허용하고 Dequeue 시 stale한 항목(더 이상 최단 거리가 아닌 항목)은 건너뛴다.
        private double[] ComputeDistanceMap(OccupancyGrid inflated, Point goal)
        {
            int cellCount = _width * _height;
            double[] distance = new double[cellCount];
            Array.Fill(distance, double.MaxValue);

            if (!inflated.IsInside(goal.X, goal.Y))
            {
                return distance;
            }

            int goalIdx = goal.Y * _width + goal.X;
            distance[goalIdx] = 0.0;

            PriorityQueue<int, double> open = new();
            open.Enqueue(goalIdx, 0.0);

            while (open.TryDequeue(out int idx, out double priority))
            {
                if (priority > distance[idx])
                {
                    continue;
                }

                int cx = idx % _width;
                int cy = idx / _width;

                foreach ((int dx, int dy, double cost) in Neighbors)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (!inflated.IsInside(nx, ny) || inflated.IsOccupied(nx, ny))
                    {
                        continue;
                    }

                    // 대각 이동은 두 직교 인접 셀(코너)이 모두 비어 있을 때만 허용한다.
                    // 그렇지 않으면 장애물 모서리를 스치듯 통과(corner cutting)하는 비현실적인 경로가 생긴다.
                    if (dx != 0 && dy != 0)
                    {
                        bool corner1Blocked = inflated.IsInside(cx + dx, cy) && inflated[cx + dx, cy];
                        bool corner2Blocked = inflated.IsInside(cx, cy + dy) && inflated[cx, cy + dy];
                        if (corner1Blocked || corner2Blocked)
                        {
                            continue;
                        }
                    }

                    int neighborIdx = ny * _width + nx;
                    double candidate = distance[idx] + cost;
                    if (candidate < distance[neighborIdx])
                    {
                        distance[neighborIdx] = candidate;
                        open.Enqueue(neighborIdx, candidate);
                    }
                }
            }

            return distance;
        }
    }
}
