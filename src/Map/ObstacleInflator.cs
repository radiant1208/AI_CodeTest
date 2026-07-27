using System;
using System.Collections.Generic;

namespace PathSearch.Map
{
    /// <summary>point-robot 근사: 장애물을 지정 반경만큼 부풀린 OccupancyGrid 생성(부정확하지만 O(1) 조회, 빠름).
    /// Holonomic 휴리스틱의 Distance Map 전처리, FootprintCollisionChecker의 1차 Bounding-Circle Early-Out 양쪽에서 재사용된다.</summary>
    public static class ObstacleInflator
    {
        private static readonly int[] OffsetX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] OffsetY = { -1, -1, -1, 0, 0, 1, 1, 1 };

        /// <summary>모든 장애물 셀을 시작점으로 한 다중 시작 BFS(8방향, Chebyshev 거리 근사)로 radiusPx 이내 셀을 장애물로 표시한다.</summary>
        public static OccupancyGrid Inflate(OccupancyGrid grid, double radiusPx)
        {
            ArgumentNullException.ThrowIfNull(grid);
            if (radiusPx < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusPx), "반경(radiusPx)은 0 이상이어야 합니다.");
            }

            int width = grid.Width;
            int height = grid.Height;
            OccupancyGrid inflated = new(width, height);

            if (radiusPx == 0)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        inflated[x, y] = grid[x, y];
                    }
                }

                return inflated;
            }

            int radiusCells = (int)Math.Ceiling(radiusPx);
            int cellCount = width * height;
            int[] distance = new int[cellCount];
            Array.Fill(distance, int.MaxValue);

            // 각 셀은 BFS 특성상 최단 거리 확정 시 단 한 번만 큐에 들어가므로 cellCount로 미리 용량을 잡아
            // Queue<T> 내부 배열의 재할당(더블링)을 없앤다. 좌표는 튜플 대신 flat index(y*width+x)로 저장.
            Queue<int> queue = new(cellCount);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!grid[x, y])
                    {
                        continue;
                    }

                    int idx = y * width + x;
                    distance[idx] = 0;
                    inflated[x, y] = true;
                    queue.Enqueue(idx);
                }
            }

            while (queue.Count > 0)
            {
                int currentIdx = queue.Dequeue();
                int currentDistance = distance[currentIdx];
                if (currentDistance >= radiusCells)
                {
                    continue;
                }

                int cx = currentIdx % width;
                int cy = currentIdx / width;

                for (int k = 0; k < OffsetX.Length; k++)
                {
                    int nx = cx + OffsetX[k];
                    int ny = cy + OffsetY[k];
                    if (!inflated.IsInside(nx, ny))
                    {
                        continue;
                    }

                    int neighborIdx = ny * width + nx;
                    int nextDistance = currentDistance + 1;
                    if (distance[neighborIdx] <= nextDistance)
                    {
                        continue;
                    }

                    distance[neighborIdx] = nextDistance;
                    inflated[nx, ny] = true;
                    queue.Enqueue(neighborIdx);
                }
            }

            return inflated;
        }
    }
}
