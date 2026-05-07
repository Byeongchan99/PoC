namespace POC8
{
    /// <summary>
    /// 데미지를 받을 수 있는 객체가 구현해야 하는 인터페이스.
    /// Enemy 등 피격 대상 클래스에서 구현한다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 지정한 양만큼 데미지를 입힌다.
        /// </summary>
        void TakeDamage(int damage);

        /// <summary>
        /// 현재 생존 여부. 체력이 1 이상이면 true.
        /// </summary>
        bool IsAlive { get; }
    }
}
