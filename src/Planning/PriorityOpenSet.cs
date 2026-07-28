using System.Collections.Generic;

namespace PathSearch.Planning
{
    /// <summary>f=g+h가 최소인 노드를 Pop하는 OpenSet. NodePool의 int 인덱스만 큐에 저장해(HybridState 객체를
    /// 직접 들고 있지 않음) 힙 할당 없이 동작한다. PriorityQueue&lt;TElement,TPriority&gt;는 decrease-key를
    /// 지원하지 않으므로, 더 나은 g가 발견될 때마다 중복 push를 허용하고 stale(더 이상 최선이 아닌) 노드는
    /// StateDiscretizer 기준으로 Pop 시점에 걸러낸다. 내부적으로 이미 배열 기반 Binary Min-Heap이라 Enqueue/Dequeue는
    /// O(log n)이며, 병목은 힙 구조 자체가 아니라 중복 엔트리 수 → NodePool 크기이므로 그쪽을 별도로 최적화했다.</summary>
    public sealed class PriorityOpenSet
    {
        private readonly PriorityQueue<int, double> _queue = new();

        /// <summary>큐에 남아있는(stale 포함) 엔트리 수</summary>
        public int Count => _queue.Count;

        public void Push(NodePool pool, int nodeIndex) => _queue.Enqueue(nodeIndex, pool[nodeIndex].F);

        /// <summary>discretizer 기준 stale 노드를 건너뛰고 f 최소 노드의 NodePool 인덱스를 반환한다. 비어있으면 -1.</summary>
        public int Pop(NodePool pool, StateDiscretizer discretizer)
        {
            while (_queue.TryDequeue(out int index, out _))
            {
                HybridStateNode node = pool[index];
                if (discretizer.IsBest(node.X, node.Y, node.ThetaRad, node.G))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
