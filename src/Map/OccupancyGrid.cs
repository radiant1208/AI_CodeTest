using System.Runtime.CompilerServices;

namespace PathSearch.Map
{
    /// <summary>픽셀 단위 2차원 점유 격자(true=장애물, false=이동 가능). 탐색 시 고빈도 조회되므로 1차원 배열로 저장.</summary>
    public sealed class OccupancyGrid
    {
        private readonly bool[] _occupied;

        /// <summary>격자 너비(단위: px)</summary>
        public int Width { get; }
        /// <summary>격자 높이(단위: px)</summary>
        public int Height { get; }

        public OccupancyGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _occupied = new bool[width * height];
        }

        public bool this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _occupied[y * Width + x];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _occupied[y * Width + x] = value;
        }

        /// <summary>좌표가 격자 범위 내부인지 여부</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>범위 밖이거나 장애물이면 true</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOccupied(int x, int y) => !IsInside(x, y) || _occupied[y * Width + x];

        /// <summary>범위 내부이면서 장애물이 아니면 true</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFree(int x, int y) => IsInside(x, y) && !_occupied[y * Width + x];
    }
}
