using NshmCalculator.Shared.Models.BaseModel;
using System.ComponentModel.DataAnnotations;
using NshmCalculator.Shared.Models.Interface;

namespace NshmCalculator.Shared.Models.CalculatorModel;

/// <summary>
/// 内功收益计算器计算数值
/// </summary>
public class KICalculateInfo : PlayerBaseInfo, ICharacterAttributes
{
    #region 新增数值
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseStamina { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseVitality { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseStrength { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseLightness { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseFullAttack { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseHalfAttack { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseElementAttack { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseRestraint { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseCriticalHits { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseBreakDefense { get; set; }
    
    [Required]
    [Range(0, 100000, ErrorMessage = "请输入0至100000内的整数")]
    public int IncreaseHit { get; set; }

    /// <summary>
    /// 新增会伤率
    /// </summary>
    [Required]
    [Range(0, 10, ErrorMessage = "请保证新增会伤百分比在0.0-10.0范围内")]
    public double IncreaseCriticalRate { get; set; }

    #endregion

    /// <summary>
    /// 攻击分
    /// </summary>
    public double Score { get; set; }

    public KICalculateInfo()
    {
        BaseAttack = 2500;
        BaseRestraint = 1000;
        BaseElementAttack = 1000;
        BaseBreakDefense = 1000;
        BaseHit = 500;
        BaseCriticalHits = 888;
        BaseCriticalRate = 188;
    }
}