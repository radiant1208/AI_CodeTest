using System;
using System.Runtime.CompilerServices;

namespace PathSearch.Planning
{
    /// <summary>탐색 노드 1개(구조체, 힙 할당 없음). 부모는 참조가 아닌 NodePool 인덱스로 연결한다(ParentIndex=-1은 루트).</summary>
    public readonly struct HybridStateNode
    {
        /// <summary>x좌표(px)</summary>
        public double X { get; }
        /// <summary>y좌표(px)</summary>
        public double Y { get; }
        /// <summary>heading(rad)</summary>
        public double ThetaRad { get; }
        /// <summary>시작부터 현재까지의 실제 누적 비용(px 환산)</summary>
        public double G { get; }
        /// <summary>현재부터 목표까지의 추정 비용(px 환산)</summary>
        public double H { get; }
        /// <summary>f = g + h</summary>
        public double F => G + H;
        /// <summary>이 노드로 이동한 동작이 후진인지 여부</summary>
        public bool IsReverse { get; }
        /// <summary>이 노드로 이동한 조향각(rad)</summary>
        public double SteeringAngleRad { get; }
        /// <summary>부모 노드의 NodePool 인덱스(루트면 -1)</summary>
        public int ParentIndex { get; }

        public HybridStateNode(double x, double y, double thetaRad, double g, double h, bool isReverse, double steeringAngleRad, int parentIndex)
        {
            X = x;
            Y = y;
            ThetaRad = thetaRad;
            G = g;
            H = h;
            IsReverse = isReverse;
            SteeringAngleRad = steeringAngleRad;
            ParentIndex = parentIndex;
        }
    }

    /// <summary>탐색 중 확장되는 노드를 구조체 배열(Arena)에 순차 저장한다. 확장마다(최대 수백만 건/탐색) 발생하던
    /// HybridState 힙 할당을 제거하기 위한 것으로, Gen0 GC를 유발하는 다수의 소형 객체 대신 소수의 대형 배열(내부적으로
    /// LOH에 올라가되 배열 자체는 Add 호출마다가 아니라 용량 초과 시에만 더블링되므로 재할당 빈도가 훨씬 낮음)만 사용한다.
    /// PriorityOpenSet은 이 풀의 int 인덱스만 큐에 보관한다.</summary>
    public sealed class NodePool
    {
        private HybridStateNode[] _nodes;
        private int _count;

        public NodePool(int initialCapacity = 1 << 16)
        {
            _nodes = new HybridStateNode[Math.Max(4, initialCapacity)];
        }

        /// <summary>현재까지 저장된 노드 수</summary>
        public int Count => _count;

        public HybridStateNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _nodes[index];
        }

        /// <summary>노드를 풀 끝에 추가하고 인덱스를 반환한다. 용량 초과 시에만 배열을 2배로 늘린다(Add 호출마다 할당 없음).</summary>
        public int Add(double x, double y, double thetaRad, double g, double h, bool isReverse, double steeringAngleRad, int parentIndex)
        {
            if (_count == _nodes.Length)
            {
                Array.Resize(ref _nodes, _nodes.Length * 2);
            }

            _nodes[_count] = new HybridStateNode(x, y, thetaRad, g, h, isReverse, steeringAngleRad, parentIndex);
            return _count++;
        }
    }
}
