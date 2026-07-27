namespace PathSearch.Planning
{
    /// <summary>Hybrid A* 탐색 노드: 연속 pose(x,y,thetaRad)와 g/h 코스트, 부모 링크를 갖는다.</summary>
    public sealed class HybridState
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
        /// <summary>역추적용 부모 노드(시작 노드는 null)</summary>
        public HybridState? Parent { get; }

        public HybridState(double x, double y, double thetaRad, double g, double h, bool isReverse, double steeringAngleRad, HybridState? parent)
        {
            X = x;
            Y = y;
            ThetaRad = thetaRad;
            G = g;
            H = h;
            IsReverse = isReverse;
            SteeringAngleRad = steeringAngleRad;
            Parent = parent;
        }
    }
}
