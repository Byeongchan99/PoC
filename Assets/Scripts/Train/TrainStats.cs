using UnityEngine;

/// <summary>
/// 기차의 기본 스탯을 담는 ScriptableObject.
/// 프로젝트 창에서 우클릭 > Create > Train > TrainStats 로 생성 가능.
/// 칸 수에 따라 속도와 최대 화물이 선형으로 증가하는 구조.
/// </summary>
[CreateAssetMenu(fileName = "TrainStats", menuName = "Train/TrainStats")]
public class TrainStats : ScriptableObject
{
    [Header("이동 설정")]
    [Tooltip("칸이 없을 때의 기본 이동 속도 (유닛/초)")]
    [SerializeField] private float _baseSpeed = 5f;

    [Tooltip("칸 1개당 추가되는 이동 속도")]
    [SerializeField] private float _speedPerCar = 0.3f;

    [Tooltip("마우스 방향으로 회전하는 최대 속도 (도/초)")]
    [SerializeField] private float _rotationSpeed = 120f;

    [Header("화물 설정")]
    [Tooltip("칸이 없을 때의 기본 최대 화물 용량")]
    [SerializeField] private int _baseCargo = 50;

    [Tooltip("칸 1개당 추가되는 최대 화물 용량")]
    [SerializeField] private int _cargoPerCar = 25;

    [Header("기차 칸 설정")]
    [Tooltip("기차 칸 사이의 간격 (유닛). 칸 크기에 맞게 조정 필요")]
    [SerializeField] private float _carSpacing = 1.2f;

    /// <summary>칸 수에 따른 이동 속도를 계산하여 반환.</summary>
    public float CalculateSpeed(int carCount) => _baseSpeed + carCount * _speedPerCar;

    /// <summary>칸 수에 따른 최대 화물 용량을 계산하여 반환.</summary>
    public int CalculateMaxCargo(int carCount) => _baseCargo + carCount * _cargoPerCar;

    public float RotationSpeed => _rotationSpeed;
    public float CarSpacing => _carSpacing;
}
