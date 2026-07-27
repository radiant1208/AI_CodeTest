using System.Collections.Generic;

namespace PathSearch.Planning
{
    /// <summary>f=g+h가 최소인 노드를 Pop하는 OpenSet. PriorityQueue&lt;TElement,TPriority&gt;는 decrease-key를
    /// 지원하지 않으므로, 더 나은 g가 발견될 때마다 중복 push를 허용하고 stale(더 이상 최선이 아닌) 노드는
    /// StateDiscretizer 기준으로 Pop 시점에 걸러낸다.</summary>
    public sealed class PriorityOpenSet
    {
        private readonly PriorityQueue<HybridState, double> _queue = new();

        /// <summary>큐에 남아있는(stale 포함) 노드 수</summary>
        public int Count => _queue.Count;

        public void Push(HybridState state) => _queue.Enqueue(state, state.F);

        /// <summary>discretizer 기준 stale 노드를 건너뛰고 f 최소 노드를 반환한다. 비어있으면 null.</summary>
        public HybridState? Pop(StateDiscretizer discretizer)
        {
            while (_queue.TryDequeue(out HybridState? state, out _))
            {
                if (discretizer.IsBest(state!.X, state.Y, state.ThetaRad, state.G))
                {
                    return state;
                }
            }

            return null;
        }
    }
}
