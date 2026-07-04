using UnityEngine;

public enum CardType
{
    Battle,     // 生成战斗单位
    Passive,    // 抽到自动部署，移出本局循环
    Terrain     // 部署后消失，生成维持整局的地形
}

[CreateAssetMenu(fileName = "Card_", menuName = "AnchorMaster/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private string _cardName = "Card";
    [SerializeField] private CardType _cardType = CardType.Battle;
    [SerializeField] [TextArea(2, 4)] private string _description;

    // Battle card unit stats
    [SerializeField] private string _unitName;
    [SerializeField] private int _hp = 10;
    [SerializeField] private int _attack = 3;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _spawnDelay;

    public string CardName => _cardName;
    public CardType CardType => _cardType;
    public string Description => _description;
    public string UnitName => _unitName;
    public int Hp => _hp;
    public int Attack => _attack;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;
    public float MoveSpeed => _moveSpeed;
    public float SpawnDelay => _spawnDelay;

    public SpawnEntry ToSpawnEntry(bool isOurUnit)
    {
        return new SpawnEntry
        {
            UnitName = _unitName,
            IsOurUnit = isOurUnit,
            SpawnDelay = _spawnDelay,
            Hp = _hp,
            Attack = _attack,
            AttackSpeed = _attackSpeed,
            AttackRange = _attackRange,
            MoveSpeed = _moveSpeed
        };
    }
}
